using Microsoft.AspNetCore.Mvc;
using RedRat3ControllerWebServer.Services;

namespace RedRat3ControllerWebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly RedRatService _redRatService;
    private readonly SerialPortService _serialPortService;
    private readonly CameraService _cameraService;
    private readonly AudioStreamingService _audioService;

    public StatusController(
        RedRatService redRatService,
        SerialPortService serialPortService,
        CameraService cameraService,
        AudioStreamingService audioService)
    {
        _redRatService = redRatService;
        _serialPortService = serialPortService;
        _cameraService = cameraService;
        _audioService = audioService;
    }

    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            redRat = new
            {
                isConnected = _redRatService.IsConnected,
                status = _redRatService.Status
            },
            serialPort = new
            {
                isConnected = _serialPortService.IsConnected,
                currentPort = _serialPortService.CurrentPort
            },
            camera = new
            {
                isRunning = _cameraService.IsRunning,
                availableCameras = _cameraService.GetAvailableCameras()
            },
            audio = new
            {
                isStreaming = _audioService.IsStreaming,
                availableDevices = _audioService.GetAvailableInputDevices()
            },
            timestamp = DateTime.Now
        });
    }
}