using Microsoft.Win32;

namespace QuickHotkeyLauncher.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "QuickHotkeyLauncher";

    public bool IsEnabled()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = runKey?.GetValue(ValueName)?.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
            ?? throw new InvalidOperationException("Unable to access startup registry key.");

        if (enabled)
        {
            var exePath = Application.ExecutablePath;
            runKey.SetValue(ValueName, $"\"{exePath}\"");
        }
        else
        {
            runKey.DeleteValue(ValueName, false);
        }
    }
}
