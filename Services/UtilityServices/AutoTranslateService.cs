﻿using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog;

namespace AbsoluteBot.Services.UtilityServices;

/// <summary>
///     Сервис для автоматического перевода сообщений пользователей.
/// </summary>
public partial class AutoTranslateService(TranslationService translationService) : IAsyncInitializable
{
    private const string FilePath = "auto_translate_users.json";
    private const string TranslationLanguage = "RU";
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private ConcurrentDictionary<string, bool> _autoTranslateUsers = new();

    public async Task InitializeAsync()
    {
        _autoTranslateUsers = await LoadAutoTranslateUsersAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Определяет, включен ли автоматический перевод для пользователя.
    /// </summary>
    /// <param name="username">Имя пользователя, для которого проверяется статус перевода.</param>
    /// <returns>Возвращает <c>true</c>, если перевод включен; иначе <c>false</c>.</returns>
    public bool IsUserAutoTranslating(string username)
    {
        try
        {
            return _autoTranslateUsers.TryGetValue(username, out var isAutoTranslating) && isAutoTranslating;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при определении включен ли режим перевода у пользователя.");
            return false;
        }
    }

    /// <summary>
    ///     Асинхронно переключает статус автоматического перевода для пользователя.
    /// </summary>
    /// <param name="username">Имя пользователя, для которого переключается статус перевода.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    public async Task<bool> ToggleUserAutoTranslateAsync(string username)
    {
        try
        {
            if (_autoTranslateUsers.TryGetValue(username, out var value))
                _autoTranslateUsers[username] = !value;
            else
                _autoTranslateUsers.TryAdd(username, true);

            await SaveAutoTranslateUsersAsync(_autoTranslateUsers).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при переключении режима перевода для пользователя.");
            return false;
        }
    }

    /// <summary>
    ///     Выполняет автоматический перевод сообщения пользователя, если перевод включен.
    /// </summary>
    /// <param name="username">Имя пользователя, от которого получено сообщение.</param>
    /// <param name="message">Сообщение, подлежащее переводу.</param>
    /// <returns>Переведенное сообщение или <c>null</c>, если перевод не был выполнен или не нужен.</returns>
    public async Task<string?> TranslateUserMessageAsync(string username, string message)
    {
        if (!_autoTranslateUsers.TryGetValue(username, out var value) || !value) return null;
        var translatedText = await translationService.TranslateTextAsync(message, TranslationLanguage).ConfigureAwait(false);

        if (translatedText == null) return null;
        var filteredOriginal = EnRuRegex().Replace(message, "");
        var filteredTranslated = EnRuRegex().Replace(translatedText, "");

        // Проверяется, отличается ли переведенный текст от оригинального, если отличается, то присылается переведённое сообщение, если нет то null
        return filteredTranslated.Equals(filteredOriginal, StringComparison.InvariantCultureIgnoreCase)
            ? null
            : translatedText;
    }

    [GeneratedRegex("[^a-zA-Zа-яА-Я]")]
    private static partial Regex EnRuRegex();

    /// <summary>
    ///     Асинхронно загружает информацию о пользователях для автоматического перевода.
    /// </summary>
    private static async Task<ConcurrentDictionary<string, bool>> LoadAutoTranslateUsersAsync()
    {
        await Semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(FilePath))
            {
                Log.Warning("Не удалось загрузить список автопереводимых пользователей, создание нового.");
                return new ConcurrentDictionary<string, bool>();
            }

            var json = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ConcurrentDictionary<string, bool>>(json) ?? new ConcurrentDictionary<string, bool>();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Ошибка при загрузке списка пользователей для автоматического перевода.");
            return new ConcurrentDictionary<string, bool>();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    ///     Асинхронно сохраняет информацию о пользователях для автоматического перевода.
    /// </summary>
    /// <param name="autoTranslateUsers">Словарь автопереводимых пользователей.</param>
    private static async Task SaveAutoTranslateUsersAsync(ConcurrentDictionary<string, bool> autoTranslateUsers)
    {
        await Semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(autoTranslateUsers, JsonOptions);
            var tempFilePath = Path.GetTempFileName();

            await File.WriteAllTextAsync(tempFilePath, json).ConfigureAwait(false);
            await Task.Run(() => File.Move(tempFilePath, FilePath, true)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при сохранении списка пользователей для автоматического перевода.");
        }
        finally
        {
            Semaphore.Release();
        }
    }
}