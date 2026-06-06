using System.IO;
using System.Text.Json;
using BrightTime.Models;

namespace BrightTime.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private readonly LogService _log;

    public SettingsService(LogService log)
    {
        _log = log;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrightTime");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load settings: {ex.Message}");
        }

        _log.Info("Creating default settings");
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            _log.Info("Settings saved");
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to save settings: {ex.Message}");
        }
    }
}
