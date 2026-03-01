using System.Globalization;

namespace QuickHotkeyLauncher.Localization;

public enum LanguageMode
{
    System = 0,
    Chinese = 1,
    English = 2
}

internal static class L
{
    private static LanguageMode _mode = LanguageMode.System;

    public static LanguageMode Mode => _mode;

    public static void SetMode(LanguageMode mode)
    {
        _mode = mode;
    }

    public static string T(string en, string zh)
    {
        return ResolveIsZh() ? zh : en;
    }

    private static bool ResolveIsZh()
    {
        if (_mode == LanguageMode.Chinese)
        {
            return true;
        }

        if (_mode == LanguageMode.English)
        {
            return false;
        }

        return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
    }
}
