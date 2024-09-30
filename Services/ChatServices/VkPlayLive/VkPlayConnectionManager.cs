using AbsoluteBot.Services.UtilityServices;
using Serilog;

namespace AbsoluteBot.Services.ChatServices.VkPlayLive;

/// <summary>
///     Управляет подключением к сервису VkPlayLive, включая логику подключения, переподключения и отключения.
/// </summary>
public class VkPlayConnectionManager(WebSocketConnectionManager webSocketManager, ConfigService configService)
{
    private const int ReconnectDelayMilliseconds = 5000;
    private const string VkPlayLiveUrl = "https://live.vkplay.ru";
    private readonly Uri _vkPlayUri = new("wss://pubsub.live.vkplay.ru/connection/websocket?cf_protocol_version=v2");
    private bool _isConfigured;
    private bool _isReconnecting;
    private string? _authToken;
    private string? _channelId;
    public bool IsConnected => webSocketManager.IsConnected;
    public event EventHandler? OnReconnectSuccess;

    /// <summary>
    ///     Асинхронно подключает к WebSocket-серверу VkPlayLive и подписывается на чат-канал.
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
                    // Отправляется сообщения для подключения и подписки на канал
                    await webSocketManager.SendMessageAsync("{\"connect\":{\"token\":\"" + _authToken + "\",\"name\":\"js\"},\"id\":1}")
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
    ///     Отключает соединение с сервером VkPlayLive.
    /// </summary>
    public async Task DisconnectAsync()
    {
        await webSocketManager.DisconnectAsync().ConfigureAwait(false);
    }

    public async Task<bool> InitializeAsync()
    {
        _authToken = await configService.GetConfigValueAsync<string>("VkPlayAuthToken").ConfigureAwait(false);
        _channelId = await configService.GetConfigValueAsync<string>("VkPlayChannelId").ConfigureAwait(false);
        if (string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_channelId))
        {
            Log.Warning("Не удалось загрузить данные аутентификации в VkPlayLive.");
            return false;
        }

        _isConfigured = true;
        return true;
    }

    /// <summary>
    ///     Асинхронно переподключает к WebSocket-серверу VkPlayLive.
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