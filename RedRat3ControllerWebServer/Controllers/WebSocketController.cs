using Microsoft.AspNetCore.Mvc;
using RedRat3ControllerWebServer.WebSocketHandlers;

namespace RedRat3ControllerWebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebSocketController : ControllerBase
{
    private readonly StatusWebSocketHandler _statusWebSocketHandler;
    private readonly AudioWebSocketHandler _audioWebSocketHandler;

    public WebSocketController(
        StatusWebSocketHandler statusWebSocketHandler,
        AudioWebSocketHandler audioWebSocketHandler)
    {
        _statusWebSocketHandler = statusWebSocketHandler;
        _audioWebSocketHandler = audioWebSocketHandler;
    }

    [HttpGet("status")]
    public async Task GetStatusWebSocket()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _statusWebSocketHandler.HandleWebSocketAsync(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    [HttpGet("audio")]
    public async Task GetAudioWebSocket()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            string clientId = Guid.NewGuid().ToString();
            await _audioWebSocketHandler.HandleWebSocketAsync(webSocket, clientId);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    [HttpPost("audio/volume")]
    public IActionResult SetAudioVolume([FromBody] SetVolumeRequest request)
    {
        _audioWebSocketHandler.SetVolume(request.ClientId, request.Volume);
        return Ok(new { message = $"Volume set to {request.Volume}" });
    }

    public class SetVolumeRequest
    {
        public string ClientId { get; set; } = "";
        public int Volume { get; set; } = 100;
    }
}