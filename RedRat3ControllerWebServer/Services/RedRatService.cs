using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AvDeviceDb;
using RedRat.Util.Serialization;

namespace RedRat3ControllerWebServer.Services;

public class RedRatService
{
    private IRedRat3? rr3;
    private XmlDeserializationResult<AVDeviceDB>? signalDB;
    private bool isConnected = false;
    private string status = "Disconnected";
    private readonly object _lock = new object();

    public event Action<string>? OnStatusChanged;
    public event Action<string, string>? OnSignalSent;

    public bool IsConnected
    {
        get { lock (_lock) { return isConnected; } }
    }

    public string Status
    {
        get { lock (_lock) { return status; } }
    }

    public void Initialize()
    {
        try
        {
            rr3 = FindRedRat();
            rr3.Connect();
            
            lock (_lock)
            {
                isConnected = true;
                status = "Connected";
            }
            
            signalDB = Serializer.AvDeviceDbFromXmlFile("REDRAT.xml");
            
            OnStatusChanged?.Invoke("Connected - Database loaded");
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                isConnected = false;
                status = $"Error: {ex.Message}";
            }
            OnStatusChanged?.Invoke($"Error - {ex.Message}");
        }
    }

    public void SendIRSignal(string deviceName, string signalName)
    {
        lock (_lock)
        {
            if (!isConnected || rr3 == null)
            {
                throw new Exception("RedRat3 is not connected");
            }

            try
            {
                var signal = GetSignal(deviceName, signalName);
                rr3.OutputModulatedSignal(signal);
                OnSignalSent?.Invoke(deviceName, signalName);
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Error sending signal: {ex.Message}");
                throw;
            }
        }
    }

    private IRedRat3 FindRedRat()
    {
        if (!(RRUtil.GetDefaultUsbRedRat() is IRedRat3 rr))
        {
            throw new Exception("Unable to find any USB RedRat devices attached to this computer.");
        }
        return rr;
    }

    private IRPacket GetSignal(string deviceName, string signalName)
    {
        var avdevice = signalDB?.Object as AVDeviceDB;
        var device = avdevice?.GetAVDevice(deviceName);
        if (device == null)
        {
            throw new Exception($"No device of name '{deviceName}' found in the signal database.");
        }
        var signal = device.GetSignal(signalName);
        if (signal == null)
        {
            throw new Exception($"No signal of name '{signalName}' found for device '{deviceName}' in the signal database.");
        }
        return signal;
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            if (rr3 != null)
            {
                try
                {
                    rr3.Disconnect();
                }
                catch { }
                rr3 = null;
            }
            isConnected = false;
            status = "Disconnected";
        }
        OnStatusChanged?.Invoke("Disconnected");
    }
}