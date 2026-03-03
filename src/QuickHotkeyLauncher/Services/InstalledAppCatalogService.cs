using Microsoft.Win32;
using QuickHotkeyLauncher.Localization;
using QuickHotkeyLauncher.Models;

namespace QuickHotkeyLauncher.Services;

public sealed class InstalledAppCatalogService
{
    private sealed class ShortcutLaunchInfo
    {
        public string TargetPath { get; init; } = string.Empty;
        public string Arguments { get; init; } = string.Empty;
    }

    public List<AppCatalogItem> GetInstalledApps()
    {
        var results = new Dictionary<string, AppCatalogItem>(StringComparer.OrdinalIgnoreCase);
        ReadFromStartMenu(results);
        ReadFromRegistry(results);
        return results.Values
            .Where(x => File.Exists(x.ExePath))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void ReadFromStartMenu(Dictionary<string, AppCatalogItem> sink)
    {
        var dirs = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs")
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var linkFile in Directory.EnumerateFiles(dir, "*.lnk", SearchOption.AllDirectories))
            {
                var shortcut = ResolveShortcutTarget(linkFile);
                if (shortcut is null)
                {
                    continue;
                }

                var target = shortcut.TargetPath;
                if (string.IsNullOrWhiteSpace(target) || !target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!File.Exists(target))
                {
                    continue;
                }

                if (!sink.ContainsKey(target))
                {
                    sink[target] = new AppCatalogItem
                    {
                        Name = Path.GetFileNameWithoutExtension(linkFile),
                        ExePath = target,
                        LaunchArguments = shortcut.Arguments,
                        Source = L.T("Start Menu", "开始菜单")
                    };
                }
            }
        }
    }

    private static void ReadFromRegistry(Dictionary<string, AppCatalogItem> sink)
    {
        var roots = new[]
        {
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
            Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
        };

        foreach (var root in roots)
        {
            using (root)
            {
                if (root is null)
                {
                    continue;
                }

                foreach (var subKeyName in root.GetSubKeyNames())
                {
                    using var sub = root.OpenSubKey(subKeyName);
                    if (sub is null)
                    {
                        continue;
                    }

                    var name = sub.GetValue("DisplayName")?.ToString();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var displayIcon = sub.GetValue("DisplayIcon")?.ToString();
                    var installLocation = sub.GetValue("InstallLocation")?.ToString();
                    var exePath = ResolveExePath(displayIcon, installLocation);
                    if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                    {
                        continue;
                    }

                    if (!sink.ContainsKey(exePath))
                    {
                        sink[exePath] = new AppCatalogItem
                        {
                            Name = name.Trim(),
                            ExePath = exePath,
                            Source = L.T("Registry", "注册表")
                        };
                    }
                }
            }
        }
    }

    private static string ResolveExePath(string? displayIcon, string? installLocation)
    {
        var path = NormalizeDisplayIconPath(displayIcon);
        if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
        {
            var exes = Directory.EnumerateFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly).ToList();
            if (exes.Count == 1)
            {
                return exes[0];
            }
        }

        return string.Empty;
    }

    private static string NormalizeDisplayIconPath(string? displayIcon)
    {
        if (string.IsNullOrWhiteSpace(displayIcon))
        {
            return string.Empty;
        }

        var value = displayIcon.Trim().Trim('"');
        var commaIndex = value.IndexOf(',');
        if (commaIndex > 0)
        {
            value = value[..commaIndex];
        }

        return value.Trim().Trim('"');
    }

    private static ShortcutLaunchInfo? ResolveShortcutTarget(string shortcutPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return null;
            }

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                return null;
            }

            dynamic? shortcut = shell.CreateShortcut(shortcutPath);
            string targetPath = shortcut.TargetPath ?? string.Empty;
            string arguments = shortcut.Arguments ?? string.Empty;
            return new ShortcutLaunchInfo
            {
                TargetPath = targetPath,
                Arguments = arguments
            };
        }
        catch
        {
            return null;
        }
    }
}
