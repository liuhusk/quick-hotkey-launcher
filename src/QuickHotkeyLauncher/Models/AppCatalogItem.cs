namespace QuickHotkeyLauncher.Models;

public sealed class AppCatalogItem
{
    public string Name { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string LaunchArguments { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Name} ({Source})";
    }
}
