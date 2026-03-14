using Microsoft.AspNetCore.Mvc;
using RedRat3ControllerWebServer.Services;

namespace RedRat3ControllerWebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedRatController : ControllerBase
{
    private readonly RedRatService _redRatService;

    public RedRatController(RedRatService redRatService)
    {
        _redRatService = redRatService;
    }

    [HttpGet("signals")]
    public IActionResult GetAvailableSignals()
    {
        try
        {
            var signals = new
            {
                device = "tMate",
                signals = new[]
                {
                    "Power", "Menu", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
                    "Up", "Down", "Left", "Right", "OK", "Back", "Exit",
                    "Mute", "VolumeUp", "VolumeDown", "ChannelUp", "ChannelDown",
                    "Red", "Green", "Yellow", "Blue"
                }
            };
            return Ok(signals);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("send")]
    public IActionResult SendSignal([FromBody] SendSignalRequest request)
    {
        try
        {
            _redRatService.SendIRSignal(request.DeviceName, request.SignalName);
            return Ok(new { message = $"Signal sent: {request.SignalName}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public class SendSignalRequest
    {
        public string DeviceName { get; set; } = "tMate";
        public string SignalName { get; set; } = "";
    }
}