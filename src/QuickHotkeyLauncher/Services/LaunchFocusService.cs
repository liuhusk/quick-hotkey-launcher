using System.Diagnostics;
using System.Text;

namespace QuickHotkeyLauncher.Services;

public sealed class LaunchFocusService
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuickHotkeyLauncher",
        "diagnostic.log");

    public void LaunchOrFocus(string exePath, string? appName = null)
    {
        var exeName = Path.GetFileNameWithoutExtension(exePath);
        Log($"trigger exePath='{exePath}', appName='{appName ?? string.Empty}'");

        if (TryGetForegroundWindowForApp(exePath, exeName, appName, out var foregroundWindow, out var foregroundReason))
        {
            Log($"action=minimize hwnd=0x{foregroundWindow.ToInt64():X}, reason={foregroundReason}");
            ForceMinimizeWindow(foregroundWindow);
            return;
        }

        if (TryFindAppWindow(exePath, exeName, appName, out var appWindow, out var appReason))
        {
            Log($"action=focus hwnd=0x{appWindow.ToInt64():X}, reason={appReason}");
            RestoreAndBringToFront(appWindow);
            return;
        }

        Log("action=start_process");
        Process.Start(exePath);
    }

    private static void RestoreAndBringToFront(IntPtr hWnd)
    {
        if (NativeMethods.IsIconic(hWnd))
        {
            NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SwRestore);
        }
        else if (NativeMethods.IsZoomed(hWnd))
        {
            // Keep maximized windows maximized when bringing them to front.
            NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SwShowMaximized);
        }
        else
        {
            NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SwShow);
        }

        if (NativeMethods.SetForegroundWindow(hWnd))
        {
            return;
        }

        var foreground = NativeMethods.GetForegroundWindow();
        var currentThread = NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var targetThread = NativeMethods.GetWindowThreadProcessId(hWnd, out _);

        if (currentThread != 0 && targetThread != 0)
        {
            NativeMethods.AttachThreadInput(currentThread, targetThread, true);
            NativeMethods.SetForegroundWindow(hWnd);
            NativeMethods.BringWindowToTop(hWnd);
            NativeMethods.AttachThreadInput(currentThread, targetThread, false);
        }
    }

    private static bool TryGetForegroundWindowForApp(string exePath, string exeName, string? appName, out IntPtr foregroundWindow, out string reason)
    {
        reason = "none";
        foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            reason = "foreground_none";
            return false;
        }

        var rootWindow = NativeMethods.GetAncestor(foregroundWindow, NativeMethods.GaRoot);
        if (rootWindow != IntPtr.Zero)
        {
            foregroundWindow = rootWindow;
        }

        return IsWindowMatchTarget(foregroundWindow, exePath, exeName, appName, out reason);
    }

    private static bool TryFindAppWindow(string exePath, string exeName, string? appName, out IntPtr window, out string reason)
    {
        IntPtr result = IntPtr.Zero;
        string foundReason = "none";
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
            {
                return true;
            }

            if (IsWindowMatchTarget(hWnd, exePath, exeName, appName, out var currentReason))
            {
                result = hWnd;
                foundReason = currentReason;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        window = result;
        reason = foundReason;
        return window != IntPtr.Zero;
    }

    private static bool IsWindowMatchTarget(IntPtr hWnd, string exePath, string exeName, string? appName, out string reason)
    {
        reason = "none";
        NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0)
        {
            reason = "pid_zero";
            return false;
        }

        try
        {
            using var process = Process.GetProcessById((int)pid);
            var processPath = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(processPath) && PathsEqual(processPath, exePath))
            {
                reason = "path_match";
                return true;
            }

            if (string.Equals(process.ProcessName, exeName, StringComparison.OrdinalIgnoreCase))
            {
                reason = "process_name_match";
                return true;
            }

            if (IsHostProcessMatch(exeName, process.ProcessName))
            {
                reason = "host_process_match";
                return true;
            }

            if (!string.IsNullOrWhiteSpace(processPath) && SameDirectory(processPath, exePath))
            {
                reason = "same_directory_match";
                return true;
            }
        }
        catch
        {
            // Ignore process inspection failures and fallback to title matching.
        }

        if (MatchesWindowTitle(hWnd, appName, exeName))
        {
            reason = "title_match";
            return true;
        }

        reason = "not_match";
        return false;
    }

    private static bool IsHostProcessMatch(string targetExeName, string actualProcessName)
    {
        if (string.IsNullOrWhiteSpace(targetExeName) || string.IsNullOrWhiteSpace(actualProcessName))
        {
            return false;
        }

        var target = targetExeName.Trim();
        var actual = actualProcessName.Trim();

        // PowerShell may run inside Windows Terminal (including Ubuntu/current tab).
        if ((target.Equals("powershell", StringComparison.OrdinalIgnoreCase) ||
             target.Equals("pwsh", StringComparison.OrdinalIgnoreCase)) &&
            (actual.Equals("WindowsTerminal", StringComparison.OrdinalIgnoreCase) ||
             actual.Equals("wt", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static void ForceMinimizeWindow(IntPtr hWnd)
    {
        // Prefer force minimize path for snappier behavior and reduced animation.
        NativeMethods.ShowWindow(hWnd, NativeMethods.SwForceMinimize);
        NativeMethods.ShowWindow(hWnd, NativeMethods.SwMinimize);
        NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SwForceMinimize);
        NativeMethods.PostMessage(hWnd, NativeMethods.WmSysCommand, (IntPtr)NativeMethods.ScMinimize, IntPtr.Zero);
    }

    private static bool MatchesWindowTitle(IntPtr hWnd, string? appName, string exeName)
    {
        var title = GetWindowTitle(hWnd);
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(appName) &&
            title.Contains(appName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (title.Contains(exeName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Console windows often use localized titles instead of process names.
        if ((exeName.Equals("powershell", StringComparison.OrdinalIgnoreCase) ||
             exeName.Equals("pwsh", StringComparison.OrdinalIgnoreCase)) &&
            title.Contains("PowerShell", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = NativeMethods.GetWindowTextLength(hWnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(length + 1);
        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static bool SameDirectory(string pathA, string pathB)
    {
        try
        {
            var dirA = Path.GetDirectoryName(Path.GetFullPath(pathA));
            var dirB = Path.GetDirectoryName(Path.GetFullPath(pathB));
            return !string.IsNullOrWhiteSpace(dirA) &&
                   !string.IsNullOrWhiteSpace(dirB) &&
                   string.Equals(dirA, dirB, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool PathsEqual(string pathA, string pathB)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(pathA).Trim(),
                Path.GetFullPath(pathB).Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ignore all logging errors to keep hotkey path safe.
        }
    }
}
