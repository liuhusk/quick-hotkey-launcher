namespace QuickHotkeyLauncher.UI;

internal static class AppBranding
{
    public const string AppName = "QuickHotkeyLauncher";
    private const string IconResourceName = "QuickHotkeyLauncher.Assets.app-icon.ico";

    public static Icon CreateIcon()
    {
        var assembly = typeof(AppBranding).Assembly;
        using var stream = assembly.GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource: {IconResourceName}");
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }
}