using Microsoft.AspNetCore.Mvc;
using RedRat3ControllerWebServer.Services;

namespace RedRat3ControllerWebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioController : ControllerBase
{
    private readonly AudioStreamingService _audioService;

    public AudioController(AudioStreamingService audioService)
    {
        _audioService = audioService;
    }

    [HttpGet("devices")]
    public IActionResult GetDevices()
    {
        var devices = _audioService.GetAvailableInputDevices();
        return Ok(new { devices });
    }

    [HttpPost("start")]
    public IActionResult StartStreaming([FromBody] StartAudioRequest request)
    {
        try
        {
            var deviceIndex = request?.DeviceIndex ?? 0;
            var deviceName = _audioService.StartStreaming(deviceIndex);
            return Ok(new { success = true, deviceName });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("stop")]
    public IActionResult StopStreaming()
    {
        _audioService.StopStreaming();
        return Ok(new { success = true });
    }

    public class StartAudioRequest
    {
        public int DeviceIndex { get; set; }
    }
}
