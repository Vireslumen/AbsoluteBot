using System.Net.WebSockets;
using System.Text;
using Serilog;

namespace AbsoluteBot.Services.UtilityServices;

/// <summary>
///     Управляет подключениями WebSocket, обеспечивая возможность отправки и получения сообщений.
/// </summary>
public class WebSocketConnectionManager
{
    private const int BufferSize = 1024 * 8;
    private readonly ClientWebSocket _webSocket = new();
    public bool IsConnected => _webSocket.State == WebSocketState.Open;

    /// <summary>
    ///     Подключает WebSocket к указанному URI с заданным заголовком Origin.
    /// </summary>
    /// <param name="uri">URI для подключения.</param>
    /// <param name="origin">Дополнительный заголовок Origin.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task ConnectAsync(Uri uri, string origin = "")
    {
        if (_webSocket.State == WebSocketState.Open) await DisconnectAsync().ConfigureAwait(false);

        _webSocket.Options.AddSubProtocol("websocket");
        if (!string.IsNullOrEmpty(origin)) _webSocket.Options.SetRequestHeader("Origin", origin);
        await _webSocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    ///     Отключает WebSocket, если он подключен.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    public async Task DisconnectAsync()
    {
        try
        {
            if (_webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.ForContext("ConnectionEvent", true).Error(ex, "Ошибка при закрытии WebSocket.");
        }
    }

    /// <summary>
    ///     Асинхронно получает сообщение от WebSocket.
    /// </summary>
    /// <returns>Полученное сообщение в виде строки.</returns>
    public async Task<string> ReceiveMessageAsync()
    {
        var buffer = new byte[BufferSize];
        var stringBuilder = new StringBuilder();
        WebSocketReceiveResult result;

        do
        {
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
            stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        } while (!result.EndOfMessage); // Чтение до конца сообщения

        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Асинхронно отправляет сообщение через WebSocket.
    /// </summary>
    /// <param name="message">Сообщение для отправки.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SendMessageAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
    }
}