using Microsoft.AspNetCore.Mvc;
using RedRat3ControllerWebServer.Services;

namespace RedRat3ControllerWebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SerialPortController : ControllerBase
{
    private readonly SerialPortService _serialPortService;

    public SerialPortController(SerialPortService serialPortService)
    {
        _serialPortService = serialPortService;
    }

    [HttpGet("ports")]
    public IActionResult GetAvailablePorts()
    {
        return Ok(new
        {
            ports = _serialPortService.GetAvailablePorts()
        });
    }

    [HttpPost("connect")]
    public IActionResult Connect([FromBody] ConnectRequest request)
    {
        try
        {
            _serialPortService.Connect(request.PortName, request.BaudRate);
            return Ok(new { message = $"Connected to {request.PortName}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("disconnect")]
    public IActionResult Disconnect()
    {
        _serialPortService.Disconnect();
        return Ok(new { message = "Disconnected" });
    }

    [HttpPost("send")]
    public IActionResult SendCommand([FromBody] SendCommandRequest request)
    {
        try
        {
            _serialPortService.SendCommand(request.Command);
            return Ok(new { message = $"Sent: {request.Command}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public class ConnectRequest
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 115200;
    }

    public class SendCommandRequest
    {
        public string Command { get; set; } = "";
    }
}