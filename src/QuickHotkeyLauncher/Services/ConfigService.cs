using System.Text.Json;
using QuickHotkeyLauncher.Models;

namespace QuickHotkeyLauncher.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _configPath;

    public ConfigService()
    {
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuickHotkeyLauncher");

        Directory.CreateDirectory(appDir);
        _configPath = Path.Combine(appDir, "config.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var tempPath = _configPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Copy(tempPath, _configPath, true);
        File.Delete(tempPath);
    }
}
