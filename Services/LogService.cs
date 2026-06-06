using System.Diagnostics;
using System.IO;

namespace BrightTime.Services;

public class LogService
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public LogService()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrightTime", "logs");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "brighttime.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                Debug.WriteLine(line);
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
        catch
        {
        }
    }
}
