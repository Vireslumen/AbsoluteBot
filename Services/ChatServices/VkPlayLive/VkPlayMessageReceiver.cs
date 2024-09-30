using AbsoluteBot.Services.UtilityServices;
using Serilog;

namespace AbsoluteBot.Services.ChatServices.VkPlayLive;

/// <summary>
///     Отвечает за получение сообщений из VkPlayLive через WebSocket.
/// </summary>
public class VkPlayMessageReceiver
{
    private readonly VkPlayConnectionManager _connectionManager;
    private readonly WebSocketConnectionManager _webSocketManager;
    private bool _isReceivingMessages;

    public VkPlayMessageReceiver(WebSocketConnectionManager webSocketManager, VkPlayConnectionManager connectionManager)
    {
        _webSocketManager = webSocketManager;
        _connectionManager = connectionManager;

        // Подписка на событие успешного переподключения
        _connectionManager.OnReconnectSuccess += (_, _) => { StartReceivingMessages(); };
    }

    /// <summary>
    ///     Проверяет, подключен ли WebSocket к серверу VkPlayLive.
    /// </summary>
    public bool IsConnected => _connectionManager.IsConnected;
    public event EventHandler<string>? OnMessageReceived;

    /// <summary>
    ///     Запускает процесс получения сообщений, если он еще не был запущен.
    /// </summary>
    public async void StartReceivingMessages()
    {
        if (_isReceivingMessages) return;
        _isReceivingMessages = true;

        try
        {
            await ReceiveMessagesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при получении сообщения из VkPlayLive.");
            // Попытка переподключения после ошибки
            if (!IsConnected)
            {
                Log.Warning("Соединение потеряно. Инициация переподключения...");
                await _connectionManager.ReconnectAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _isReceivingMessages = false;
        }
    }

    /// <summary>
    ///     Асинхронно получает сообщения через WebSocket и обрабатывает их.
    /// </summary>
    private async Task ReceiveMessagesAsync()
    {
        while (_webSocketManager.IsConnected)
            try
            {
                var message = await _webSocketManager.ReceiveMessageAsync().ConfigureAwait(false);

                // Ping-Pong: если получает ping-сообщение, отправляет pong
                if (message == "{}")
                    await _webSocketManager.SendMessageAsync("{}").ConfigureAwait(false);
                else
                    // Вызывается событие для обработки полученного сообщения
                    OnMessageReceived?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при получении сообщения из VkPlayLive.");
                await _connectionManager.ReconnectAsync().ConfigureAwait(false);
            }
    }
}