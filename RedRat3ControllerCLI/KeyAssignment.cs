using System;
using System.Windows.Forms;

namespace RedRat3ControllerCLI
{
    public class KeyAssignment
    {
        public string SignalName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public Keys AssignedKey { get; set; }

        public KeyAssignment(string signalName, string displayName, Keys defaultKey)
        {
            SignalName = signalName;
            DisplayName = displayName;
            AssignedKey = defaultKey;
        }

        public string KeyToString()
        {
            return AssignedKey.ToString();
        }

        public static Keys? StringToKey(string keyString)
        {
            if (Enum.TryParse<Keys>(keyString, out Keys key))
            {
                return key;
            }
            return null;
        }
    }
}