using Microsoft.Win32;

namespace BrightTimeTray.Services;

public class StartupService
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Name = "BrightTimeTray";

    public bool IsEnabled()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Key);
            return k?.GetValue(Name) != null;
        }
        catch { return false; }
    }

    public void Enable()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Key, true);
            var exe = Environment.ProcessPath;
            if (exe != null) k?.SetValue(Name, $"\"{exe}\"");
        }
        catch { }
    }

    public void Disable()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Key, true);
            k?.DeleteValue(Name, false);
        }
        catch { }
    }
}
