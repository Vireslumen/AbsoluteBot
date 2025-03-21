using System.Text;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AbsoluteBot.Services.NeuralNetworkServices;

/// <summary>
/// Сервис для генерации изображений с помощью Gemini
/// </summary>
/// <param name="settingsProvider"></param>
public class GeminiImageGenerationService(GeminiSettingsProvider settingsProvider)
{
    /// <summary>
    /// Асинхронный запрос к модели Gemini с передачей сообщения.
    /// </summary>
    /// <param name="message">Сообщение для отправки модели.</param>
    /// <param name="base64Image">Изображение в формате Base64.</param>
    /// <returns>Ответ модели в base64 и текст или null, если возникла ошибка.</returns>
    public async Task<(string? text, string? image)> GenerateImageGeminiResponseAsync(string message, string? base64Image = null)
    {
        try
        {
            if (settingsProvider.ApiKeys == null || settingsProvider.ApiKeys.Count == 0) return (null, null);
            message = "Создай пожалуйста изображение с следующим промптом: " + message;
            string? text = null;
            string? image = null;
            foreach (var apiKey in settingsProvider.ApiKeys)
            {
                var url = $"{GeminiSettingsProvider.BaseApiUrl}/gemini-2.0-flash-exp:generateContent?key={apiKey}";

                var jsonData = GenerateJsonPayload(message, base64Image);
                var stream = await settingsProvider.FetchImageModelResponseStreamAsync(jsonData, url).ConfigureAwait(false);
                (text, image) = await ProcessImageStreamAsync(stream);
                if (string.IsNullOrEmpty(image))
                    continue;
                return (text, image);
            }

            return (text, image);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при получении ответа от Gemini.");
            return (null, null);
        }
    }

    /// <summary>
    /// Обрабатывает поток изображения, предполагая, что он может содержать как текст, так и base64 строку изображения.
    /// </summary>
    /// <param name="imageStream">Поток ответа от Gemini.</param>
    /// <returns>Кортеж, содержащий текст и base64 строку изображения (оба могут быть null).</returns>
    public async Task<(string? Text, string? Base64Image)> ProcessImageStreamAsync(Stream? imageStream)
    {
        if (imageStream == null) return (null, null);

        string? textResult = null;
        string? base64ImageResult = null;

        try
        {
            using var reader = new StreamReader(imageStream, Encoding.UTF8);
            var jsonResponseString = await reader.ReadToEndAsync().ConfigureAwait(false);

            var jsonResponse = JObject.Parse(jsonResponseString);
            var parts = jsonResponse["candidates"]?[0]?["content"]?["parts"] as JArray;

            if (parts != null)
                foreach (var part in parts)
                {
                    var textToken = part["text"];
                    if (textToken != null) textResult = textToken.ToString();

                    var inlineData = part["inlineData"] as JObject;
                    if (inlineData != null)
                    {
                        var mimeType = inlineData["mimeType"]?.ToString();
                        var data = inlineData["data"]?.ToString();
                        if (mimeType == "image/png" && !string.IsNullOrEmpty(data)) base64ImageResult = data;
                    }
                }

            return (textResult, base64ImageResult);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при обработке потока.");
            return (null, null);
        }
        finally
        {
            await imageStream.DisposeAsync().ConfigureAwait(false);
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