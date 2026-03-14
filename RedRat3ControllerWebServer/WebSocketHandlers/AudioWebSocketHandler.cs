using System.Net.WebSockets;
using RedRat3ControllerWebServer.Services;

namespace RedRat3ControllerWebServer.WebSocketHandlers;

public class AudioWebSocketHandler
{
    private readonly AudioStreamingService _audioService;

    public AudioWebSocketHandler(AudioStreamingService audioService)
    {
        _audioService = audioService;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, string clientId)
    {
        // Connect client to audio service
        _audioService.ConnectClient(clientId);

        try
        {
            var buffer = new byte[1024 * 4];

            // Stream audio data
            while (webSocket.State == WebSocketState.Open)
            {
                var audioChunk = _audioService.GetAudioChunk(clientId, 100);

                if (audioChunk != null)
                {
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(audioChunk, 0, audioChunk.Length),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);
                }
            }
        }
        catch
        {
            // Handle disconnection
        }
        finally
        {
            _audioService.DisconnectClient(clientId);
        }
    }

    public void SetVolume(string clientId, int volume)
    {
        _audioService.SetClientVolume(clientId, volume);
    }
}