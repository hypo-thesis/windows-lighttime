using System.Text.Json;

namespace BrightTime.Services;

public class SettingsService
{
    private readonly string _path;
    private readonly LogService _log;

    public SettingsService(LogService log)
    {
        _log = log;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrightTime");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    public Models.AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var s = JsonSerializer.Deserialize<Models.AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Settings load: {ex.Message}");
        }
        return new Models.AppSettings();
    }

    public void Save(Models.AppSettings s)
    {
        try
        {
            var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch (Exception ex)
        {
            _log.Error($"Settings save: {ex.Message}");
        }
    }
}
