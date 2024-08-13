using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AvDeviceDb;
using RedRat.Util.Serialization;

class RedRat3ControllerCLI
{
    // dict to map keys to ir signal by name
    private static readonly Dictionary<ConsoleKey, string> KeyToIRSignalMap = new Dictionary<ConsoleKey, string>
    {
        { ConsoleKey.V, "DTV_MUTE_K" },
        // add more mappings
    };

    static void Main(string[] args)
    {
        Console.WriteLine("### RedRat3 Controller ###\n");

        using var rr3 = FindRedRat();
        rr3.Connect();
        var signalDB = Serializer.AvDeviceDbFromXmlFile("REDRAT.xml");
        Console.WriteLine("Database initialised.");

        Console.WriteLine("Intercepting keys. Press ESC to exit.\n");

        while (true)
        {
            // capture key press
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            ConsoleKey key = keyInfo.Key;

            // break if ESC
            if (key == ConsoleKey.Escape)
            {
                Console.WriteLine("Goodbye...");
                break;
            }

            // check if key has mapped ir signal
            if (KeyToIRSignalMap.ContainsKey(key))
            {
                // then output the ir signal
                var signalName = KeyToIRSignalMap[key];
                SignalOutput(rr3, signalDB, signalName);
            }
        }

        rr3.Disconnect();
    }

    public static void SignalOutput(IRedRat3 rr, XmlDeserializationResult<AVDeviceDB> signalDB, string signalName)
    {
        try
        {

            var signal = GetSignal(signalDB, "tMate", signalName);
            rr.OutputModulatedSignal(signal);
            Console.WriteLine($"Signal {signalName} output.");


        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    private static IRedRat3 FindRedRat()
    {
        if (!(RRUtil.GetDefaultUsbRedRat() is IRedRat3 rr3))
        {
            throw new Exception("Unable to find any USB RedRat devices attached to this computer.");
        }
        Console.WriteLine("RedRat connected.");

        return rr3;
    }


    private static IRPacket GetSignal(XmlDeserializationResult<AVDeviceDB> signalDB, string deviceName, string signalName)
    {
        var avdevice = signalDB.Object as AVDeviceDB;
        var device = avdevice.GetAVDevice(deviceName);
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
}
