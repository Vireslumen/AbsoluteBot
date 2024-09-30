﻿using AbsoluteBot.Chat.Context;
using AbsoluteBot.Services.ChatServices.Discord;
using AbsoluteBot.Services.ChatServices.TelegramChat;
using AbsoluteBot.Services.ChatServices.TwitchChat;
using AbsoluteBot.Services.ChatServices.VkPlayLive;
using AbsoluteBot.Services.GoogleSheetsServices;
using AbsoluteBot.Services.NeuralNetworkServices;
using AbsoluteBot.Services.UtilityServices;
using Serilog;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;

namespace AbsoluteBot.Services.ScheduledTasks;
#pragma warning disable IDE0028
/// <summary>
///     Сервис для мониторинга стримов на Twitch и обновления информации о стриме.
/// </summary>
public class TwitchStreamMonitoringService(TwitchChatService twitchChatService, ConfigService configService,
    TelegramChatService telegramChatService, DiscordChatService discordChatService, ChatGeminiService geminiService, TelegramChannelManager telegramChannelManager,
    GameGoogleSheetsService gameGoogleSheetsService, StreamGoogleSheetsService streamGoogleSheetsService,
    GameProgressService gameProgressService, VkPlayChatService vkPlayChatService) : IAsyncInitializable
{
    private const int VkPlayReminderIntervalHours = 2;
    private const string VkPlayReminderMessage = "Напиши !команды, чтобы узнать что я могу, или тегни меня, чтобы поболтать.";
    private bool _streamWasOnline;
    private CancellationTokenSource? _vkPlayReminderCancellationTokenSource;
    private List<string>? _streamLinks = new();
    private string? _lastGameName;

    public async Task InitializeAsync()
    {
        _streamLinks = await configService.GetConfigValueAsync<List<string>>("StreamLinks").ConfigureAwait(false);
        _streamWasOnline = await configService.GetConfigValueAsync<bool>("StreamWasOnline").ConfigureAwait(false);
        _lastGameName = await configService.GetConfigValueAsync<string>("LastGameName").ConfigureAwait(false);
        if (string.IsNullOrEmpty(_lastGameName) || _streamLinks == null || _streamLinks.Count < 1)
            Log.Warning("Не удалось загрузить последнюю игру на стриме или ссылки на стрим.");
    }

    /// <summary>
    ///     Выполняет проверку состояния стрима на Twitch и обрабатывает изменения.
    /// </summary>
    public async Task ExecuteTwitchOnlineCheckTask(int twitchCheckTimeMinutes)
    {
        try
        {
            var stream = await twitchChatService.GetStreamState().ConfigureAwait(false);
            if (stream == null)
                await HandleStreamEnded().ConfigureAwait(false);
            else
                await HandleStreamStartedOrContinued(stream, twitchCheckTimeMinutes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка проверки состояния стрима на Twitch.");
        }
    }

    /// <summary>
    ///     Обрабатывает смену игры на стриме.
    /// </summary>
    /// <param name="newGameName">Название новой игры.</param>
    private async Task HandleGameChange(string newGameName)
    {
        var streamNumber = await configService.GetConfigValueAsync<int>("StreamNumber").ConfigureAwait(false);
        await UpdateGoogleSheetsForStreamStartAsync(newGameName, streamNumber).ConfigureAwait(false);
        _lastGameName = newGameName;
    }

    /// <summary>
    ///     Обрабатывает завершение стрима.
    /// </summary>
    private async Task HandleStreamEnded()
    {
        if (!_streamWasOnline) return;

        _streamWasOnline = false;
        await configService.SetConfigValueAsync("StreamWasOnline", false).ConfigureAwait(false);

        // Остановка таймера VkPlay при завершении стрима
        StopVkPlayReminder();

        // Отправка уведомления в Telegram о завершении стрима
        var telegramChatId = await telegramChannelManager.GetTelegramChannelId(ChannelType.Announce).ConfigureAwait(false);
        await telegramChatService.SendMessageToChannelAsync("Стрим окончен!", telegramChatId.ToString()).ConfigureAwait(false);
        await telegramChatService.UnPinLastMessage(telegramChatId.ToString()).ConfigureAwait(false);

        await geminiService.AddUserMessageToChatHistory("Стрим завершился!", "System").ConfigureAwait(false);
    }

    /// <summary>
    ///     Обрабатывает начало стрима на Twitch.
    /// </summary>
    /// <param name="stream">Информация о текущем стриме.</param>
    private async Task HandleStreamStarted(Stream stream)
    {
        _streamWasOnline = true;
        await configService.SetConfigValueAsync("StreamWasOnline", true).ConfigureAwait(false);

        var streamNumber = await configService.GetConfigValueAsync<int>("StreamNumber").ConfigureAwait(false);
        streamNumber++;
        await configService.SetConfigValueAsync("StreamNumber", streamNumber).ConfigureAwait(false);

        // Запуск асинхронной задачи для отправки напоминаний каждые 2 часа
        _ = StartVkPlayReminderAsync().ConfigureAwait(false);

        // Уведомление о начале стрима в Telegram и Discord
        if (_streamLinks != null)
        {
            var announceMessage = "Стрим включен!\n" + string.Join("\n", _streamLinks);
            var telegramChatId = await telegramChannelManager.GetTelegramChannelId(ChannelType.Announce).ConfigureAwait(false);
            await telegramChatService.SendPinnedMessageToChannelAsync(announceMessage, telegramChatId.ToString()).ConfigureAwait(false);
            await discordChatService.AnnounceMessage(announceMessage).ConfigureAwait(false);
        }

        // Обновление таблицы игр
        await UpdateGoogleSheetsForStreamStartAsync(stream.GameName, streamNumber).ConfigureAwait(false);

        // Уведомление самого бота о начале стрима
        await geminiService.AddUserMessageToChatHistory($"Начался стрим, игра на стриме: {stream.GameName}!", "System").ConfigureAwait(false);
    }

    /// <summary>
    ///     Обрабатывает состояние стрима при его продолжении или смене игры.
    /// </summary>
    /// <param name="stream">Информация о текущем стриме.</param>
    /// <param name="twitchCheckTimeMinutes">Время в минутах, которое проходит между вызовами обновления прогресса.</param>
    private async Task HandleStreamStartedOrContinued(Stream stream, int twitchCheckTimeMinutes)
    {
        if (!_streamWasOnline)
            await HandleStreamStarted(stream).ConfigureAwait(false);
        else if (_lastGameName != stream.GameName)
            await HandleGameChange(stream.GameName).ConfigureAwait(false);

        // Обновление прогресса игры
        await UpdateGameProgress(stream.GameName, twitchCheckTimeMinutes).ConfigureAwait(false);
    }

    /// <summary>
    ///     Запускает повторяющееся отправление напоминаний в чат VkPlay с интервалом в 2 часа.
    /// </summary>
    private async Task StartVkPlayReminderAsync()
    {
        _vkPlayReminderCancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Пока стрим активен, отправляем сообщение каждые 2 часа
            while (!_vkPlayReminderCancellationTokenSource.Token.IsCancellationRequested)
            {
                await vkPlayChatService.SendMessageToChannelAsync(VkPlayReminderMessage).ConfigureAwait(false);

                // Ожидание 2 часа или отмены через CancellationToken
                await Task.Delay(TimeSpan.FromHours(VkPlayReminderIntervalHours), _vkPlayReminderCancellationTokenSource.Token).ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
            Log.Information("Отправка сообщений на VkPlay была отменена.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при отправке напоминания на VkPlay.");
        }
    }

    /// <summary>
    ///     Останавливает отправление сообщений в VkPlay.
    /// </summary>
    private void StopVkPlayReminder()
    {
        _vkPlayReminderCancellationTokenSource?.Cancel();
    }

    /// <summary>
    ///     Обновляет прогресс игры, используя данные стрима.
    /// </summary>
    /// <param name="gameName">Название игры.</param>
    /// <param name="twitchCheckTimeMinutes">Время в минутах, которое проходит между вызовами обновления прогресса.</param>
    private async Task UpdateGameProgress(string gameName, int twitchCheckTimeMinutes)
    {
        await gameProgressService.UpdateLastGameNameAsync(gameName).ConfigureAwait(false);
        await gameProgressService.AddTimeToCurrentGameAsync(gameName, twitchCheckTimeMinutes).ConfigureAwait(false);
        await gameProgressService.CalculateGameProgressAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Обновляет данные в Google Sheets, связанные с началом нового стрима.
    /// </summary>
    private async Task UpdateGoogleSheetsForStreamStartAsync(string gameName, int streamNumber)
    {
        // Проверяется и добавляется игра в таблицу игр, если её там еще нет
        await gameGoogleSheetsService.AddOrUpdateGameWithStreamAsync(gameName, streamNumber).ConfigureAwait(false);

        // Добавляется новая строку стрима в таблицу стримов
        await streamGoogleSheetsService.AddOrUpdateStreamRowAsync(streamNumber, gameName).ConfigureAwait(false);
    }
}