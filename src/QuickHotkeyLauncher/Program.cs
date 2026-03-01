using System.Threading;
using QuickHotkeyLauncher.Forms;
using QuickHotkeyLauncher.Localization;

namespace QuickHotkeyLauncher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, "QuickHotkeyLauncher.Singleton", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                L.T("Application is already running.", "应用已在运行。"),
                "QuickHotkeyLauncher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
