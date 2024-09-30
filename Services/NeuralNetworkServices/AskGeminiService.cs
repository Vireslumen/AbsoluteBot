using Newtonsoft.Json.Linq;
using Serilog;

namespace AbsoluteBot.Services.NeuralNetworkServices;

/// <summary>
///     Сервис для работы с моделью Gemini, поддерживающий генерацию ответов на основе запроса пользователя.
/// </summary>
public class AskGeminiService(GeminiSettingsProvider settingsProvider)
{
    /// <summary>
    ///     Асинхронный запрос к модели Gemini с передачей сообщения.
    /// </summary>
    /// <param name="message">Сообщение для отправки модели.</param>
    /// <param name="maxLength">Максимальная длина ответа в символах.</param>
    /// <param name="base64Image">Изображение в формате Base64.</param>
    /// <returns>Ответ модели или null, если возникла ошибка.</returns>
    public async Task<string?> AskGeminiResponseAsync(string message, int maxLength, string? base64Image = null)
    {
        try
        {
            if (settingsProvider.ApiKeys == null || settingsProvider.ApiKeys.Count == 0) return null;
            message += $". Постарайся уложиться в {maxLength} символов. Выдай цельный ответ без форматирования.";

            var url = $"{GeminiSettingsProvider.BaseApiUrl}/{settingsProvider.Models[1]}:generateContent?key={settingsProvider.ApiKeys.First()}";

            var jsonData = GenerateJsonPayload(message, base64Image);
            var text = await settingsProvider.FetchModelResponseAsync(jsonData, url).ConfigureAwait(false);
            return !string.IsNullOrEmpty(text) ? text : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при получении ответа от Gemini.");
            return null;
        }
    }

    /// <summary>
    ///     Создание JSON-данных с сообщением и опциональной строкой изображения для отправки модели.
    /// </summary>
    /// <param name="message">Сообщение пользователя.</param>
    /// <param name="image">Строка, представляющая изображение в формате Base64. Если не указана, отправляется только текст.</param>
    /// <returns>JSON-данные для отправки модели.</returns>
    private static string GenerateJsonPayload(string message, string? image = null)
    {
        var parts = new JArray
        {
            new JObject {["text"] = message}
        };

        if (!string.IsNullOrEmpty(image))
            parts.Add(
                new JObject
                {
                    ["inlineData"] = new JObject
                    {
                        ["mimeType"] = "image/png",
                        ["data"] = image
                    }
                });

        var jsonPayload = new JObject
        {
            ["contents"] = new JArray
            {
                new JObject
                {
                    ["parts"] = parts
                }
            }
        };

        return jsonPayload.ToString();
    }
}