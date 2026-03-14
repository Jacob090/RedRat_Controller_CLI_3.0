using System.IO.Ports;

namespace RedRat3ControllerWebServer.Services;

public class SerialPortService
{
    private SerialPort? serialPort;
    private bool isConnected = false;
    private string currentPort = "None";
    private readonly object _lock = new object();

    public event Action<string>? OnDataReceived;
    public event Action<string>? OnStatusChanged;
    public event Action<string>? OnCommandSent;

    public bool IsConnected
    {
        get { lock (_lock) { return isConnected; } }
    }

    public string CurrentPort
    {
        get { lock (_lock) { return currentPort; } }
    }

    public string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    public void Connect(string portName, int baudRate = 115200)
    {
        lock (_lock)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                Disconnect();
            }

            try
            {
                serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();
                
                isConnected = true;
                currentPort = portName;
                
                OnStatusChanged?.Invoke($"Connected to {portName} at {baudRate} baud");
            }
            catch (Exception ex)
            {
                isConnected = false;
                currentPort = "None";
                OnStatusChanged?.Invoke($"Error connecting to {portName}: {ex.Message}");
                throw;
            }
        }
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.DataReceived -= SerialPort_DataReceived;
                    serialPort.Close();
                    serialPort.Dispose();
                    serialPort = null;
                }
                catch { }
            }

            isConnected = false;
            currentPort = "None";
            OnStatusChanged?.Invoke("Disconnected");
        }
    }

    public void SendCommand(string command)
    {
        lock (_lock)
        {
            if (!isConnected || serialPort == null || !serialPort.IsOpen)
            {
                throw new Exception("Serial port is not connected");
            }

            try
            {
                serialPort.Write(command + "\r\n");
                OnCommandSent?.Invoke(command);
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Error sending command: {ex.Message}");
                throw;
            }
        }
    }

    private void SerialPort_DataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadExisting();
                OnDataReceived?.Invoke(data);
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Error reading data: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}