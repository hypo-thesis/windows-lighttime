using System.Diagnostics;

namespace BrightTime.Services;

public class LogService
{
    private readonly string _path;
    private readonly object _lock = new();

    public LogService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrightTime", "logs");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "brighttime.log");
    }

    public void Info(string m) => Write("INFO", m);
    public void Warn(string m) => Write("WARN", m);
    public void Error(string m) => Write("ERROR", m);

    private void Write(string level, string m)
    {
        try
        {
            lock (_lock)
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {m}";
                Debug.WriteLine(line);
                File.AppendAllText(_path, line + Environment.NewLine);
            }
        }
        catch { }
    }
}
