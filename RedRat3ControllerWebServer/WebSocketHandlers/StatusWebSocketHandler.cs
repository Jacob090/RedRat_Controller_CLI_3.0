using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RedRat3ControllerWebServer.Services;

namespace RedRat3ControllerWebServer.WebSocketHandlers;

public class StatusWebSocketHandler
{
    private readonly RedRatService _redRatService;
    private readonly SerialPortService _serialPortService;
    private readonly CameraService _cameraService;
    private readonly AudioStreamingService _audioService;

    public StatusWebSocketHandler(
        RedRatService redRatService,
        SerialPortService serialPortService,
        CameraService cameraService,
        AudioStreamingService audioService)
    {
        _redRatService = redRatService;
        _serialPortService = serialPortService;
        _cameraService = cameraService;
        _audioService = audioService;

        // Subscribe to service events
        _redRatService.OnStatusChanged += (status) => BroadcastMessage("redrat", status);
        _redRatService.OnSignalSent += (device, signal) => BroadcastMessage("signal", $"{device}: {signal}");
        _serialPortService.OnDataReceived += (data) => BroadcastMessage("serial", $"Received: {data}");
        _serialPortService.OnCommandSent += (command) => BroadcastMessage("serial", $"Sent: {command}");
        _serialPortService.OnStatusChanged += (status) => BroadcastMessage("serial", status);
        _cameraService.OnStatusChanged += (status) => BroadcastMessage("camera", status);
        _cameraService.OnError += (error) => BroadcastMessage("error", error);
        _audioService.OnStatusChanged += (status) => BroadcastMessage("audio", status);
    }

    private static readonly List<WebSocket> _connectedClients = new();

    public async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        _connectedClients.Add(webSocket);

        try
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                // Keep connection alive
                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }
        finally
        {
            _connectedClients.Remove(webSocket);
        }
    }

    private void BroadcastMessage(string type, string message)
    {
        var messageObj = new
        {
            type = type,
            message = message,
            timestamp = DateTime.Now.ToString("HH:mm:ss")
        };

        var json = JsonSerializer.Serialize(messageObj);
        var buffer = Encoding.UTF8.GetBytes(json);

        foreach (var client in _connectedClients.Where(c => c.State == WebSocketState.Open).ToList())
        {
            try
            {
                client.SendAsync(
                    new ArraySegment<byte>(buffer, 0, buffer.Length),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch
            {
                _connectedClients.Remove(client);
            }
        }
    }
}