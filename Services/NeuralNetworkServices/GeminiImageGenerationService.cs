using Newtonsoft.Json.Linq;
using Serilog;

namespace AbsoluteBot.Services.NeuralNetworkServices;

/// <summary>
/// Сервис для генерации изображений с помощью Gemini
/// </summary>
/// <param name="settingsProvider"></param>
internal class GeminiImageGenerationService(GeminiSettingsProvider settingsProvider)
{
    /// <summary>
    /// Асинхронный запрос к модели Gemini с передачей сообщения.
    /// </summary>
    /// <param name="message">Сообщение для отправки модели.</param>
    /// <param name="maxLength">Максимальная длина ответа в символах.</param>
    /// <param name="base64Image">Изображение в формате Base64.</param>
    /// <returns>Ответ модели в base64 или null, если возникла ошибка.</returns>
    public async Task<string?> GenerateImageGeminiResponseAsync(string message, int maxLength, string? base64Image = null)
    {
        try
        {
            if (settingsProvider.ApiKeys == null || settingsProvider.ApiKeys.Count == 0) return null;
            message += $". Постарайся уложиться в {maxLength} символов. Выдай цельный ответ без форматирования.";

            var url = $"{GeminiSettingsProvider.BaseApiUrl}/gemini-2.0-flash-exp:generateContent?key={settingsProvider.ApiKeys.First()}";

            var jsonData = GenerateJsonPayload(message, base64Image);
            var image = await settingsProvider.FetchImageModelResponseAsync(jsonData, url).ConfigureAwait(false);
            return !string.IsNullOrEmpty(image) ? image : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при получении ответа от Gemini.");
            return null;
        }
    }

    /// <summary>
    /// Создание JSON-данных с сообщением и опциональной строкой изображения для отправки модели.
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
            },
            ["generationConfig"] = new JObject
            {
                ["temperature"] = 1,
                ["topP"] = 0.95,
                ["topK"] = 40,
                ["maxOutputTokens"] = 8192,
                ["responseMimeType"] = "text/plain",
                ["responseModalities"] = new JArray {"image", "text"}
            }
        };

        return jsonPayload.ToString();
    }
}