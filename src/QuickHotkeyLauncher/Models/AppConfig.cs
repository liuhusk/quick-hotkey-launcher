namespace QuickHotkeyLauncher.Models;

public sealed class AppConfig
{
    public int Version { get; set; } = 1;
    public string LanguageMode { get; set; } = "system";
    public List<AppBinding> Bindings { get; set; } = new();
}
