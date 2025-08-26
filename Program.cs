using System;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var mainForm = new MainForm();
            Application.Run();
        }
    }
}
