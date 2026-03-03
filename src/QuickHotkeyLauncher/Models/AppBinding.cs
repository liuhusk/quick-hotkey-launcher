namespace QuickHotkeyLauncher.Models;

public sealed class AppBinding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AppName { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string LaunchArguments { get; set; } = string.Empty;
    public HotkeyDefinition? Hotkey { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
