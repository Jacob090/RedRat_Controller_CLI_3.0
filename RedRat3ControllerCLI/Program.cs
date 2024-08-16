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
        { ConsoleKey.P, "DTV_POWER_K" },
        { ConsoleKey.H, "DTV_SOURCE_K" },
        { ConsoleKey.D0, "DTV_0_K" },
        { ConsoleKey.D1, "DTV_1_K" },
        { ConsoleKey.D2, "DTV_2_K" },
        { ConsoleKey.D3, "DTV_3_K" },
        { ConsoleKey.D4, "DTV_4_K" },
        { ConsoleKey.D5, "DTV_5_K" },
        { ConsoleKey.D6, "DTV_6_K" },
        { ConsoleKey.D7, "DTV_7_K" },
        { ConsoleKey.D8, "DTV_8_K" },
        { ConsoleKey.D9, "DTV_9_K" },
        { ConsoleKey.T, "DTV_TTX_MIX_K" },
        { ConsoleKey.V, "DTV_MUTE_K" },
        { ConsoleKey.C, "DTV_CH_LIST_K" },
        { ConsoleKey.Multiply, "DTV_VOL_UP_K" },
        { ConsoleKey.Divide, "DTV_VOL_DOWN_K" },
        { ConsoleKey.Add, "DTV_CH_UP_K" },
        { ConsoleKey.Subtract, "DTV_CH_DOWN_K" },
        { ConsoleKey.M, "DTV_MENU_K" },
        { ConsoleKey.S, "DTV_SMART_K" },
        { ConsoleKey.G, "DTV_GUIDE_K" },
        { ConsoleKey.UpArrow, "DTV_UP_K" },
        { ConsoleKey.DownArrow, "DTV_DOWN_K" },
        { ConsoleKey.LeftArrow, "DTV_LEFT_K" },
        { ConsoleKey.RightArrow, "DTV_RIGHT_K" },
        { ConsoleKey.Enter, "DTV_ENTER_K" },
        { ConsoleKey.I, "DTV_INFO_K" },
        { ConsoleKey.Backspace, "DTV_RETURN_K" },
        { ConsoleKey.X, "DTV_EXIT_K" },
        { ConsoleKey.F1, "DTV_RED_K" },
        { ConsoleKey.F2, "DTV_GREEN_K" },
        { ConsoleKey.F3, "DTV_YELLOW_K" },
        { ConsoleKey.F4, "DTV_BLUE_K" },
        { ConsoleKey.Q, "SPE_MORE_K" },
        { ConsoleKey.OemComma, "DTV_REWIND_K" },
        { ConsoleKey.OemPeriod, "DTV_FF_K" },
        { ConsoleKey.U, "DTV_PAUSE_K" },
        { ConsoleKey.R, "DTV_REC_K" },
        { ConsoleKey.Y, "DTV_PLAY_K" },
        { ConsoleKey.Z, "DTV_STOP_K" },
        { ConsoleKey.F, "DTV_FACTORY_K" },
    };

    static void Main(string[] args)
    {
        Console.WriteLine("### RedRat3 Controller ###\n");
        IRedRat3 rr3;

        try
        {
            rr3 = FindRedRat();
        } catch (Exception)
        {
            Console.ReadKey();
            return;
        }

        rr3.Connect();

        XmlDeserializationResult<AVDeviceDB> signalDB;
        try
        {
            signalDB = Serializer.AvDeviceDbFromXmlFile("REDRAT.xml");
        } catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.ReadKey();
            return;
        }
            
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
            Console.WriteLine("Unable to find any USB RedRat devices attached to this computer.");
            Console.WriteLine("Check USB connection and installed driver.");

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
