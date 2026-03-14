using Microsoft.AspNetCore.Mvc;

namespace RedRat3ControllerWebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KeyAssignmentsController : ControllerBase
{
    private const string SettingsFile = "key_assignments.xml";
    private readonly string _settingsPath;

    public KeyAssignmentsController(IConfiguration configuration)
    {
        // Get the content root path
        var contentRoot = configuration.GetValue<string>(WebHostDefaults.ContentRootKey);
        _settingsPath = Path.Combine(contentRoot, SettingsFile);
    }

    [HttpGet]
    public IActionResult GetKeyAssignments()
    {
        try
        {
            var assignments = LoadKeyAssignmentsFromFile();
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult SaveKeyAssignments([FromBody] List<KeyAssignment> assignments)
    {
        try
        {
            // Validate for duplicate keys
            var validationErrors = ValidateAssignments(assignments);
            if (validationErrors.Count > 0)
            {
                return BadRequest(new { errors = validationErrors });
            }

            // Save to file
            SaveKeyAssignmentsToFile(assignments);
            return Ok(new { message = "Key assignments saved successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private List<KeyAssignment> LoadKeyAssignmentsFromFile()
    {
        var assignments = GetDefaultKeyAssignments();

        if (!System.IO.File.Exists(_settingsPath))
        {
            return assignments;
        }

        try
        {
            var doc = System.Xml.Linq.XDocument.Load(_settingsPath);
            var root = doc.Element("KeyAssignments");
            if (root != null)
            {
                var loadedAssignments = root.Elements("KeyAssignment").ToList();
                
                foreach (var loaded in loadedAssignments)
                {
                    string signalName = loaded.Element("SignalName")?.Value ?? "";
                    string keyString = loaded.Element("AssignedKey")?.Value ?? "";
                    
                    var key = StringToKey(keyString);
                    if (key.HasValue)
                    {
                        // Find matching assignment and update its key
                        var assignment = assignments.FirstOrDefault(a => a.SignalName == signalName);
                        if (assignment != null)
                        {
                            assignment.AssignedKey = key.Value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading key assignments: {ex.Message}");
        }

        return assignments;
    }

    private void SaveKeyAssignmentsToFile(List<KeyAssignment> assignments)
    {
        var doc = new System.Xml.Linq.XDocument(
            new System.Xml.Linq.XElement("KeyAssignments",
                assignments.Select(ka =>
                    new System.Xml.Linq.XElement("KeyAssignment",
                        new System.Xml.Linq.XElement("SignalName", ka.SignalName),
                        new System.Xml.Linq.XElement("DisplayName", ka.DisplayName),
                        new System.Xml.Linq.XElement("AssignedKey", ka.AssignedKey.ToString())
                    )
                )
            )
        );

        doc.Save(_settingsPath);
    }

    private List<string> ValidateAssignments(List<KeyAssignment> assignments)
    {
        var errors = new List<string>();
        var usedKeys = new Dictionary<string, string>();

        foreach (var assignment in assignments)
        {
            string keyString = KeyToString(assignment.AssignedKey);
            
            if (string.IsNullOrEmpty(keyString))
            {
                errors.Add($"Invalid key for {assignment.DisplayName}");
                continue;
            }

            // Check for duplicate keys
            if (usedKeys.ContainsKey(keyString))
            {
                errors.Add($"Key '{keyString}' is assigned to both '{assignment.DisplayName}' and '{usedKeys[keyString]}'");
            }
            else
            {
                usedKeys[keyString] = assignment.DisplayName;
            }
        }

        return errors;
    }

    private string KeyToString(Keys key)
    {
        switch (key)
        {
            case Keys.D0: return "D0";
            case Keys.D1: return "D1";
            case Keys.D2: return "D2";
            case Keys.D3: return "D3";
            case Keys.D4: return "D4";
            case Keys.D5: return "D5";
            case Keys.D6: return "D6";
            case Keys.D7: return "D7";
            case Keys.D8: return "D8";
            case Keys.D9: return "D9";
            case Keys.Oemcomma: return "Oemcomma";
            case Keys.OemPeriod: return "OemPeriod";
            default:
                return key.ToString();
        }
    }

    private Keys? StringToKey(string keyString)
    {
        if (string.IsNullOrEmpty(keyString))
            return null;

        if (Enum.TryParse<Keys>(keyString, out var key))
        {
            return key;
        }

        return null;
    }

    private List<KeyAssignment> GetDefaultKeyAssignments()
    {
        return new List<KeyAssignment>
        {
            new KeyAssignment { SignalName = "DTV_POWER_K", DisplayName = "Power Key", AssignedKey = Keys.P },
            new KeyAssignment { SignalName = "DTV_SOURCE_K", DisplayName = "Source Key", AssignedKey = Keys.H },
            new KeyAssignment { SignalName = "DTV_0_K", DisplayName = "0", AssignedKey = Keys.D0 },
            new KeyAssignment { SignalName = "DTV_1_K", DisplayName = "1", AssignedKey = Keys.D1 },
            new KeyAssignment { SignalName = "DTV_2_K", DisplayName = "2", AssignedKey = Keys.D2 },
            new KeyAssignment { SignalName = "DTV_3_K", DisplayName = "3", AssignedKey = Keys.D3 },
            new KeyAssignment { SignalName = "DTV_4_K", DisplayName = "4", AssignedKey = Keys.D4 },
            new KeyAssignment { SignalName = "DTV_5_K", DisplayName = "5", AssignedKey = Keys.D5 },
            new KeyAssignment { SignalName = "DTV_6_K", DisplayName = "6", AssignedKey = Keys.D6 },
            new KeyAssignment { SignalName = "DTV_7_K", DisplayName = "7", AssignedKey = Keys.D7 },
            new KeyAssignment { SignalName = "DTV_8_K", DisplayName = "8", AssignedKey = Keys.D8 },
            new KeyAssignment { SignalName = "DTV_9_K", DisplayName = "9", AssignedKey = Keys.D9 },
            new KeyAssignment { SignalName = "DTV_TTX_MIX_K", DisplayName = "TTX/Mix Key", AssignedKey = Keys.T },
            new KeyAssignment { SignalName = "DTV_MUTE_K", DisplayName = "Mute Key", AssignedKey = Keys.V },
            new KeyAssignment { SignalName = "DTV_CH_LIST_K", DisplayName = "Channel List Key", AssignedKey = Keys.C },
            new KeyAssignment { SignalName = "DTV_VOL_UP_K", DisplayName = "Volume Up", AssignedKey = Keys.Multiply },
            new KeyAssignment { SignalName = "DTV_VOL_DOWN_K", DisplayName = "Volume Down", AssignedKey = Keys.Divide },
            new KeyAssignment { SignalName = "DTV_CH_UP_K", DisplayName = "Channel Up", AssignedKey = Keys.Add },
            new KeyAssignment { SignalName = "DTV_CH_DOWN_K", DisplayName = "Channel Down", AssignedKey = Keys.Subtract },
            new KeyAssignment { SignalName = "DTV_MENU_K", DisplayName = "Menu Key", AssignedKey = Keys.M },
            new KeyAssignment { SignalName = "DTV_SMART_K", DisplayName = "Smart Key", AssignedKey = Keys.S },
            new KeyAssignment { SignalName = "DTV_GUIDE_K", DisplayName = "Guide Key", AssignedKey = Keys.G },
            new KeyAssignment { SignalName = "DTV_UP_K", DisplayName = "Up Arrow", AssignedKey = Keys.Up },
            new KeyAssignment { SignalName = "DTV_DOWN_K", DisplayName = "Down Arrow", AssignedKey = Keys.Down },
            new KeyAssignment { SignalName = "DTV_LEFT_K", DisplayName = "Left Arrow", AssignedKey = Keys.Left },
            new KeyAssignment { SignalName = "DTV_RIGHT_K", DisplayName = "Right Arrow", AssignedKey = Keys.Right },
            new KeyAssignment { SignalName = "DTV_ENTER_K", DisplayName = "Enter Key", AssignedKey = Keys.Enter },
            new KeyAssignment { SignalName = "DTV_INFO_K", DisplayName = "Info Key", AssignedKey = Keys.I },
            new KeyAssignment { SignalName = "DTV_RETURN_K", DisplayName = "Return Key", AssignedKey = Keys.Back },
            new KeyAssignment { SignalName = "DTV_EXIT_K", DisplayName = "Exit Key", AssignedKey = Keys.X },
            new KeyAssignment { SignalName = "DTV_RED_K", DisplayName = "Red Key (F1)", AssignedKey = Keys.F1 },
            new KeyAssignment { SignalName = "DTV_GREEN_K", DisplayName = "Green Key (F2)", AssignedKey = Keys.F2 },
            new KeyAssignment { SignalName = "DTV_YELLOW_K", DisplayName = "Yellow Key (F3)", AssignedKey = Keys.F3 },
            new KeyAssignment { SignalName = "DTV_BLUE_K", DisplayName = "Blue Key (F4)", AssignedKey = Keys.F4 },
            new KeyAssignment { SignalName = "SPE_MORE_K", DisplayName = "More Key (Q)", AssignedKey = Keys.Q },
            new KeyAssignment { SignalName = "DTV_REWIND_K", DisplayName = "Rewind", AssignedKey = Keys.Oemcomma },
            new KeyAssignment { SignalName = "DTV_FF_K", DisplayName = "Fast Forward", AssignedKey = Keys.OemPeriod },
            new KeyAssignment { SignalName = "DTV_PAUSE_K", DisplayName = "Pause Key", AssignedKey = Keys.U },
            new KeyAssignment { SignalName = "DTV_REC_K", DisplayName = "Record Key", AssignedKey = Keys.R },
            new KeyAssignment { SignalName = "DTV_PLAY_K", DisplayName = "Play Key", AssignedKey = Keys.Y },
            new KeyAssignment { SignalName = "DTV_STOP_K", DisplayName = "Stop Key", AssignedKey = Keys.Z },
            new KeyAssignment { SignalName = "DTV_FACTORY_K", DisplayName = "Factory Key", AssignedKey = Keys.F },
        };
    }
}

public class KeyAssignment
{
    public string SignalName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Keys AssignedKey { get; set; }
}

[Flags]
public enum Keys
{
    None = 0,
    D0 = 0x30,
    D1 = 0x31,
    D2 = 0x32,
    D3 = 0x33,
    D4 = 0x34,
    D5 = 0x35,
    D6 = 0x36,
    D7 = 0x37,
    D8 = 0x38,
    D9 = 0x39,
    A = 0x41,
    B = 0x42,
    C = 0x43,
    D = 0x44,
    E = 0x45,
    F = 0x46,
    G = 0x47,
    H = 0x48,
    I = 0x49,
    J = 0x4A,
    K = 0x4B,
    L = 0x4C,
    M = 0x4D,
    N = 0x4E,
    O = 0x4F,
    P = 0x50,
    Q = 0x51,
    R = 0x52,
    S = 0x53,
    T = 0x54,
    U = 0x55,
    V = 0x56,
    W = 0x57,
    X = 0x58,
    Y = 0x59,
    Z = 0x5A,
    Add = 0x6B,
    Subtract = 0x6D,
    Multiply = 0x6A,
    Divide = 0x6F,
    Enter = 0x0D,
    Back = 0x08,
    Escape = 0x1B,
    Space = 0x20,
    Tab = 0x09,
    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,
    F1 = 0x70,
    F2 = 0x71,
    F3 = 0x72,
    F4 = 0x73,
    F5 = 0x74,
    F6 = 0x75,
    F7 = 0x76,
    F8 = 0x77,
    F9 = 0x78,
    F10 = 0x79,
    F11 = 0x7A,
    F12 = 0x7B,
    Oemcomma = 0xBC,
    OemPeriod = 0xBE
}