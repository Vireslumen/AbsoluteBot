﻿using System.Text;
using AbsoluteBot.Services.UtilityServices;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AbsoluteBot.Services.NeuralNetworkServices;
#pragma warning disable IDE0300
/// <summary>
///     Предоставляет настройки и методы для взаимодействия с моделью Gemini, включая доступ к API ключам и моделям.
/// </summary>
public class GeminiSettingsProvider(ConfigService configService, HttpClient httpClient) : IAsyncInitializable
{
    public const string BaseApiUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    public readonly string[] Models = {"gemini-1.5-pro-latest", "gemini-1.5-flash-latest"};
    public List<string>? ApiKeys;

    public async Task InitializeAsync()
    {
        ApiKeys = await configService.GetConfigValueAsync<List<string>>("GeminiApiKeys").ConfigureAwait(false);
        if (ApiKeys == null || ApiKeys.Count < 1)
            Log.Warning("Не удалось загрузить api ключи для gemini.");
    }

    /// <summary>
    ///     Отправка HTTP-запроса к модели и получение текста ответа.
    /// </summary>
    /// <param name="jsonData">JSON-данные запроса.</param>
    /// <param name="url">URL для отправки запроса.</param>
    /// <returns>Текстовый ответ модели или null в случае ошибки.</returns>
    public async Task<string?> FetchModelResponseAsync(string jsonData, string url)
    {
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, url) {Content = content};
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var text = ParseResponse(result);
        return text;
    }

    /// <summary>
    ///     Извлечение текста ответа из JSON-ответа модели.
    /// </summary>
    /// <param name="result">Ответ от модели в формате JSON.</param>
    /// <returns>Извлеченный текст ответа или null, если текст не найден.</returns>
    private static string? ParseResponse(string result)
    {
        var jsonResponse = JObject.Parse(result);
        return jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
    }
}