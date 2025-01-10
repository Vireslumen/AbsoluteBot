using System.Text.RegularExpressions;
using AbsoluteBot.Services.UtilityServices;
using Serilog;

namespace AbsoluteBot.Services.ChatServices.VkPlayLive;

/// <summary>
/// Управляет подключением к сервису VkPlayLive, включая логику подключения, переподключения и отключения.
/// </summary>
public class VkPlayConnectionManager(WebSocketConnectionManager webSocketManager, ConfigService configService,
    VkPlayMessageSender vkPlayMessageSender)
{
    private const int ReconnectDelayMilliseconds = 5000;
    private const string VkPlayLiveUrl = "https://live.vkvideo.ru";
    private readonly Uri _vkPlayUri = new("wss://pubsub.live.vkvideo.ru/connection/websocket?cf_protocol_version=v2");
    private bool _isConfigured;
    private bool _isReconnecting;
    private string? _authCookie;
    private string? _channelId;
    public bool IsConnected => webSocketManager.IsConnected;
    public event EventHandler? OnReconnectSuccess;

    /// <summary>
    /// Асинхронно подключает к WebSocket-серверу VkPlayLive и подписывается на чат-канал.
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            while (!IsConnected && _isConfigured)
            {
                // Подключение к WebSocket-серверу
                await webSocketManager.ConnectAsync(_vkPlayUri, VkPlayLiveUrl).ConfigureAwait(false);

                if (IsConnected)
                {
                    var (readToken, authCookie, sendToken) = await FetchTokenAuthAndAccessTokenFromUrl(VkPlayLiveUrl, _authCookie);
                    File.WriteAllText("text1.txt", readToken + sendToken + authCookie);
                    if (authCookie != null)
                    {
                        _authCookie = authCookie;
                        await configService.SetConfigValueAsync("VkPlayAuthToken", authCookie);
                    }

                    if (sendToken != null)
                        vkPlayMessageSender.SetAuthSendToken(sendToken);
                    // Отправляется сообщения для подключения и подписки на канал
                    await webSocketManager.SendMessageAsync("{\"connect\":{\"token\":\"" + readToken + "\",\"name\":\"js\"},\"id\":1}")
                        .ConfigureAwait(false);
                    await webSocketManager.SendMessageAsync("{\"subscribe\":{\"channel\":\"channel-chat:" + _channelId + "\"},\"id\":2}")
                        .ConfigureAwait(false);
                }

                // Уведомление, что переподключение было успешным
                OnReconnectSuccess?.Invoke(this, EventArgs.Empty);
                if (!IsConnected)
                    await Task.Delay(ReconnectDelayMilliseconds).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.ForContext("ConnectionEvent", true).Error(ex, "Ошибка при подключении ");
            throw;
        }
    }

    /// <summary>
    /// Отключает соединение с сервером VkPlayLive.
    /// </summary>
    public async Task DisconnectAsync()
    {
        await webSocketManager.DisconnectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Асинхронно загружает HTML-код с указанного URL и извлекает значения токена, auth и accessToken.
    /// Передает необходимые куки в запросе.
    /// </summary>
    /// <param name="url">URL веб-страницы для загрузки HTML-кода.</param>
    /// <returns>Кортеж из значения токена, строки auth в формате куков и значения accessToken.</returns>
    public static async Task<(string? token, string? auth, string? accessToken)> FetchTokenAuthAndAccessTokenFromUrl(string url, string authCookie)
    {
        try
        {
            using var client = new HttpClient();

            // Установка заголовков и куков
            client.DefaultRequestHeaders.Add("Cookie", authCookie);

            // Загрузка HTML-кода по URL
            var html = await client.GetStringAsync(url);
            File.WriteAllText("text13.txt", html);
            // Регулярное выражение для поиска значения токена
            const string tokenPattern = "\"token\":\"(.*?)\"";
            var tokenMatch = Regex.Match(html, tokenPattern);
            var token = tokenMatch.Success ? tokenMatch.Groups[1].Value : null;

            if (token == null) return (null, null, null);
            // Регулярное выражение для поиска auth
            const string authPattern = "\"auth\":\\{(.*?)\\}";
            var authMatch = Regex.Match(html, authPattern);
            var auth = authMatch.Success ? $"\"auth\":{{{authMatch.Groups[1].Value}}};" : null;

            if (auth == null) return (null, null, null);
            auth = auth.Replace("\"auth\":", "auth=");
            auth = auth.Replace("access", "accessToken");
            auth = auth.Replace("refresh", "refreshToken");
            auth = auth.Replace("expiresIn", "expiresAt");

            // Регулярное выражение для поиска accessToken
            const string accessTokenPattern = "\"accessToken\":\"(.*?)\"";
            var accessTokenMatch = Regex.Match(auth, accessTokenPattern);
            var accessToken = accessTokenMatch.Success ? accessTokenMatch.Groups[1].Value : null;

            if (accessToken == null) return (null, null, null);
            // Возврат значений токена, auth и accessToken
            return (token, auth, accessToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при обновлении ключей для vklive.");
            return (null, null, null);
        }
    }

    public async Task<bool> InitializeAsync()
    {
        _authCookie = await configService.GetConfigValueAsync<string>("VkPlayAuthToken").ConfigureAwait(false);
        _channelId = await configService.GetConfigValueAsync<string>("VkPlayChannelId").ConfigureAwait(false);
        if (string.IsNullOrEmpty(_authCookie) || string.IsNullOrEmpty(_channelId))
        {
            Log.Warning("Не удалось загрузить данные аутентификации в VkPlayLive.");
            return false;
        }

        _isConfigured = true;
        return true;
    }

    /// <summary>
    /// Асинхронно переподключает к WebSocket-серверу VkPlayLive.
    /// </summary>
    public async Task ReconnectAsync()
    {
        if (_isReconnecting) return;
        _isReconnecting = true;

        try
        {
            Log.ForContext("ConnectionEvent", true).Information("Попытка переподключения к VkPlayLive...");
            await ConnectAsync().ConfigureAwait(false);
            Log.ForContext("ConnectionEvent", true).Information("Переподключение к VkPlayLive успешно.");
            _isReconnecting = false;
            return;
        }
        catch (Exception ex)
        {
            Log.ForContext("ConnectionEvent", true).Error(ex, "Ошибка при попытке переподключения.");
        }

        _isReconnecting = false;
    }
}