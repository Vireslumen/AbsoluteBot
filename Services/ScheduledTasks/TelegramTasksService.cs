using AbsoluteBot.Chat.Context;
using AbsoluteBot.Services.ChatServices.TelegramChat;
using AbsoluteBot.Services.MediaServices;
using AbsoluteBot.Services.NeuralNetworkServices;
using Serilog;

namespace AbsoluteBot.Services.ScheduledTasks;

/// <summary>
///     Сервис для выполнения ежедневных задач в Telegram, таких как отправка информации о праздниках и курсе валют.
/// </summary>
public class TelegramTasksService(TelegramChannelManager telegramChannelManager, TelegramChatService telegramChatService,
    HolidaysService holidaysService, ChatGptService chatGptService, ExchangeRateService exchangeRateService)
{
    private const string DateFormat = "dd.MM";
    private const int MaxFactLength = 200;

    /// <summary>
    ///     Выполняет ежедневные задачи в Telegram, такие как отправка информации о праздниках и курсе валют.
    /// </summary>
    public async Task ExecuteDailyTelegramTask()
    {
        try
        {
            var channelId = await telegramChannelManager.GetTelegramChannelId(ChannelType.Premium).ConfigureAwait(false);

            await HolidayHandler(channelId).ConfigureAwait(false);

            await ChartHandler(channelId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при выполнении ежедневных задач в Telegram.");
        }
    }

    /// <summary>
    ///     Отправляет график курса валют в Telegram.
    /// </summary>
    /// <param name="channelId">Идентификатор канала в Telegram.</param>
    private async Task ChartHandler(long channelId)
    {
        var chartUrl = await exchangeRateService.GetExchangeRateChartUrlAsync().ConfigureAwait(false);
        if (chartUrl == null) return;
        await telegramChatService.SendPhotoToChannelAsync(chartUrl, channelId.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    ///     Отправляет информацию о празднике и интересный факт в Telegram.
    /// </summary>
    /// <param name="channelId">Идентификатор канала в Telegram.</param>
    private async Task HolidayHandler(long channelId)
    {
        var today = DateTime.Today.ToString(DateFormat);
        var holiday = holidaysService.GetHoliday(today);
        var fact = await chatGptService.AskChatGptAsync($"Расскажи интересный факт про {holiday}, который отмечается {today}.", MaxFactLength)
            .ConfigureAwait(false);
        await telegramChatService.SendMessageToChannelAsync($"Сегодня праздник: {holiday}\nВот интересный факт: {fact}", channelId.ToString())
            .ConfigureAwait(false);
    }
}