using Microsoft.AspNetCore.Mvc;
using RedRat3ControllerWebServer.Services;

namespace RedRat3ControllerWebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CameraController : ControllerBase
{
    private readonly CameraService _cameraService;

    public CameraController(CameraService cameraService)
    {
        _cameraService = cameraService;
    }

    [HttpGet("cameras")]
    public IActionResult GetAvailableCameras()
    {
        return Ok(new
        {
            cameras = _cameraService.GetAvailableCameras()
        });
    }

    [HttpPost("start")]
    public IActionResult StartCamera([FromBody] StartCameraRequest request)
    {
        try
        {
            _cameraService.StartCamera(request.CameraIndex);
            return Ok(new { message = "Camera started" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("stop")]
    public IActionResult StopCamera()
    {
        _cameraService.StopCamera();
        return Ok(new { message = "Camera stopped" });
    }

    [HttpGet("frame")]
    public IActionResult GetLatestFrame()
    {
        var frame = _cameraService.GetLatestFrame();
        if (frame == null)
        {
            return NotFound(new { error = "No frame available" });
        }

        return File(frame, "image/jpeg");
    }

    public class StartCameraRequest
    {
        public int CameraIndex { get; set; } = 0;
    }
}