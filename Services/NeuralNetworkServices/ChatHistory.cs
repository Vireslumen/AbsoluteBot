using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AbsoluteBot.Services.NeuralNetworkServices;

#pragma warning disable IDE0028
/// <summary>
///     Класс <c>ChatHistory</c> предназначен для хранения и управления историей сообщений чата.
/// </summary>
public class ChatHistory
{
    private const int MaxMessages = 200; // Сколько сообщений хранит бот в памяти
    private const string FilePath = "ChatHistory_{0}.json";
    private const int SaveThreshold = 20; // Каждые сколько сообщений история чата сохраняется
    private const int RecentMessageCount = 7; // Сколько последних сообщений считаются последними сообщениями в чате
    private readonly JArray _messages = new();
    private readonly SemaphoreSlim _messageSemaphore = new(1, 1);
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);

    /// <summary>
    ///     Асинхронно добавляет начальные сообщения пользователя и модели в историю чата.
    /// </summary>
    /// <param name="userMessage">Начальное сообщение пользователя.</param>
    /// <param name="modelMessage">Начальное сообщение модели.</param>
    /// <param name="platform">Платформа, на которой используется история чата.</param>
    public async Task AddInitialMessagesAsync(string userMessage, string modelMessage, string platform)
    {
        _messages.Add(CreateMessage("user", userMessage));
        _messages.Add(CreateMessage("model", modelMessage));
        await SaveMessagesIfNeededAsync(platform).ConfigureAwait(false);
    }

    /// <summary>
    ///     Асинхронно добавляет сообщение от роли в историю чата.
    /// </summary>
    /// <param name="role">Роль отправителя (обычно "model" или "user").</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="base64Image">
    ///     Строка, представляющая изображение в формате Base64. Если не указана, отправляется
    ///     только текст.
    /// </param>
    /// <param name="platform">Платформа, на которой используется история чата.</param>
    public async Task AddMessageAsync(string role, string text, string platform, string? base64Image = null)
    {
        await _messageSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (base64Image != null) await RemoveImagesFromHistoryAsync(platform);
            _messages.Add(CreateMessage(role, text, base64Image));
            TrimHistory();
            await SaveMessagesIfNeededAsync(platform).ConfigureAwait(false);
        }
        finally
        {
            _messageSemaphore.Release();
        }
    }

    /// <summary>
    ///     Асинхронно добавляет или обновляет сообщение от модели в истории чата.
    ///     Если сообщение уже присутствует в истории, оно перемещается в конец списка.
    /// </summary>
    /// <param name="role">Роль отправителя (обычно "model").</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="platform">Платформа, на которой используется история чата.</param>
    public async Task AddOrUpdateModelMessageAsync(string role, string text, string platform)
    {
        await _messageSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var existingIndex = GetLastMessageIndexInRecentHistory(role, text, MaxMessages);
            if (existingIndex > 2)
            {
                // Перемещает существующее сообщение в конец списка
                var messageToMove = _messages[existingIndex];
                _messages.RemoveAt(existingIndex);
                _messages.Add(messageToMove);

                // Перемещает предшествующее сообщение, если оно существует и является сообщением пользователя
                if (_messages[existingIndex - 1]["role"]?.ToString() == "user")
                {
                    var precedingMessage = _messages[existingIndex - 1];
                    _messages.RemoveAt(existingIndex - 1);
                    _messages.Insert(_messages.Count - 1, precedingMessage); // Вставляет перед моделью
                }
            }
            else
            {
                _messages.Add(CreateMessage(role, text));
            }

            TrimHistory();
            await SaveMessagesIfNeededAsync(platform).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при добавлении ответного сообщения в историю чата.");
        }
        finally
        {
            _messageSemaphore.Release();
        }
    }

    /// <summary>
    ///     Асинхронно добавляет или обновляет сообщение от пользователя в истории чата.
    ///     Если сообщение уже присутствует в истории, оно перемещается в конец списка.
    /// </summary>
    /// <param name="role">Роль отправителя (обычно "user").</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="platform">Платформа, на которой используется история чата.</param>
    public async Task AddOrUpdateUserMessageAsync(string role, string text, string platform)
    {
        await _messageSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var existingIndex = GetLastMessageIndexInRecentHistory(role, text, RecentMessageCount);
            if (existingIndex != -1)
            {
                // Перемещает существующее сообщение в конец списка
                var messageToMove = _messages[existingIndex];
                _messages.RemoveAt(existingIndex);
                _messages.Add(messageToMove);
            }
            else
            {
                _messages.Add(CreateMessage(role, text));
            }

            TrimHistory();
            await SaveMessagesIfNeededAsync(platform).ConfigureAwait(false);
        }
        finally
        {
            _messageSemaphore.Release();
        }
    }

    /// <summary>
    ///     Асинхронно очищает историю чата, кроме двух первых сообщений.
    /// </summary>
    public async Task ClearExceptFirstTwo()
    {
        await _messageSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            while (_messages.Count > 2) _messages.RemoveAt(2);
        }
        finally
        {
            _messageSemaphore.Release();
        }
    }

    /// <summary>
    ///     Возвращает текущую историю чата в формате <see cref="JArray" />.
    /// </summary>
    /// <returns>История сообщений в формате <see cref="JArray" />.</returns>
    public JArray GetHistory()
    {
        return _messages;
    }

    /// <summary>
    ///     Проверяет, есть ли сообщение в недавней истории.
    /// </summary>
    /// <param name="role">Роль отправителя (обычно "user" или "model").</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="messagesCount">Количество последних сообщений для проверки.</param>
    /// <returns><c>true</c>, если сообщение присутствует в недавней истории; иначе <c>false</c>.</returns>
    public bool IsMessageInRecentHistory(string role, string text, int messagesCount = RecentMessageCount)
    {
        var recentMessagesCount = Math.Min(messagesCount, _messages.Count);
        for (var i = _messages.Count - recentMessagesCount; i < _messages.Count; i++)
        {
            var message = _messages[i];
            if (message["role"]?.ToString() == role &&
                message["parts"]?[0]?["text"]?.ToString() ==
                text) return true;
        }

        return false;
    }

    /// <summary>
    ///     Асинхронно загружает начальные сообщения для истории чата из файлов.
    /// </summary>
    /// <param name="userMessageFilePath">Путь к файлу с сообщением пользователя.</param>
    /// <param name="modelMessageFilePath">Путь к файлу с сообщением модели.</param>
    /// <param name="platform">Платформа, на которой используется история чата.</param>
    public async Task LoadInitialMessagesFromFileAsync(string userMessageFilePath, string modelMessageFilePath,
        string platform)
    {
        if (!File.Exists(userMessageFilePath))
            await File.WriteAllTextAsync(userMessageFilePath,
                "Описание работы нейросети... (добавьте сюда весь необходимый текст)").ConfigureAwait(false);

        if (!File.Exists(modelMessageFilePath))
            await File.WriteAllTextAsync(modelMessageFilePath, "Бот: Привет! Я готов помочь.").ConfigureAwait(false);

        var userMessage = await File.ReadAllTextAsync(userMessageFilePath).ConfigureAwait(false);
        var modelMessage = await File.ReadAllTextAsync(modelMessageFilePath).ConfigureAwait(false);

        await AddInitialMessagesAsync(userMessage, modelMessage, platform).ConfigureAwait(false);
    }

    /// <summary>
    ///     Создает объект сообщения в формате JSON, который добавляется в историю сообщений.
    /// </summary>
    /// <param name="role">Роль, связанная с сообщением (например, "user" или "model").</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="base64Image">
    ///     Строка, представляющая изображение в формате Base64. Если не указана, отправляется только
    ///     текст.
    /// </param>
    /// <returns>Объект сообщения в формате JSON.</returns>
    private static JObject CreateMessage(string role, string text, string? base64Image = null)
    {
        var parts = new JArray {new JObject {["text"] = text}};

        if (!string.IsNullOrEmpty(base64Image))
            parts.Add(new JObject
            {
                ["inlineData"] = new JObject
                {
                    ["mimeType"] = "image/png",
                    ["data"] = base64Image
                }
            });
        var message = new JObject
        {
            ["role"] = role,
            ["parts"] = parts
        };

        return message;
    }

    /// <summary>
    ///     Ищет последний индекс сообщения с указанной ролью и текстом в недавней истории сообщений.
    ///     Возвращает индекс последнего найденного совпадения, если такое сообщение существует, иначе возвращает -1.
    ///     Метод проверяет только последние messagesCount сообщений в истории.
    /// </summary>
    /// <param name="role">Роль, связанная с сообщением (например, "user" или "model").</param>
    /// <param name="text">Текст сообщения для поиска.</param>
    /// <param name="messagesCount">Максимальное количество сообщений для проверки.</param>
    /// <returns>Последний индекс сообщения в списке сообщений или -1, если сообщение не найдено.</returns>
    private int GetLastMessageIndexInRecentHistory(string role, string text, int messagesCount)
    {
        var recentMessagesCount = Math.Min(messagesCount, _messages.Count);
        for (var i = _messages.Count - 1; i >= _messages.Count - recentMessagesCount; i--)
        {
            var message = _messages[i];
            if (IsMatchingMessage(message, role, text))
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Проверяет, соответствует ли сообщение заданной роли и тексту.
    /// </summary>
    /// <param name="message">Сообщение для проверки.</param>
    /// <param name="role">Ожидаемая роль сообщения (например, "user" или "model").</param>
    /// <param name="text">Ожидаемый текст сообщения.</param>
    /// <returns>true, если роль и текст сообщения совпадают с заданными значениями, иначе false.</returns>
    private static bool IsMatchingMessage(JToken? message, string role, string text)
    {
        return message?["role"]?.ToString() == role &&
               message["parts"]?[0]?["text"]?.ToString() == text;
    }

    /// <summary>
    ///     Удаляет все изображения из истории сообщений.
    /// </summary>
    /// <param name="platform">Платформа, на которой надо удалить изображения.</param>
    private async Task RemoveImagesFromHistoryAsync(string platform)
    {
        foreach (var message in _messages)
        {
            if (message["parts"] is not JArray parts) continue;
            for (var i = parts.Count - 1; i >= 0; i--)
                if (parts[i] is JObject part && part["inlineData"] != null)
                    parts.RemoveAt(i);
        }

        await SaveMessagesToFileAsync(platform).ConfigureAwait(false);
    }

    /// <summary>
    ///     Асинхронно сохраняет сообщения в файл, если достигнут порог количества сообщений.
    /// </summary>
    /// <param name="platform">Платформа, на которой используется история чата.</param>
    private async Task SaveMessagesIfNeededAsync(string platform)
    {
        if (_messages.Count % SaveThreshold == 0) await SaveMessagesToFileAsync(platform).ConfigureAwait(false);
    }

    /// <summary>
    ///     Асинхронно сохраняет историю сообщений в файл.
    /// </summary>
    /// <param name="platform">Платформа, на которой используется история чата.</param>
    private async Task SaveMessagesToFileAsync(string platform)
    {
        await _fileSemaphore.WaitAsync().ConfigureAwait(false); // Блокировка доступа к файлу
        try
        {
            var jsonHistory = JsonConvert.SerializeObject(_messages, Formatting.Indented);
            await File.WriteAllTextAsync(string.Format(FilePath, platform), jsonHistory).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при сохранении истории сообщений в файл.");
        }
        finally
        {
            _fileSemaphore.Release(); // Разблокировка доступа к файлу
        }
    }

    /// <summary>
    ///     Обрезает историю сообщений, если она превышает допустимый максимум.
    ///     Удаляет третий и четвертый элементы в списке сообщений, если общее количество сообщений больше MaxMessages.
    /// </summary>
    private void TrimHistory()
    {
        while (_messages.Count > MaxMessages) _messages.RemoveAt(2);
    }
}