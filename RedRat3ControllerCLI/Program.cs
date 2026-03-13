using System;
using System.Windows.Forms;

namespace RedRat3ControllerCLI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RedRat3ControllerForm());
        }
    }
}