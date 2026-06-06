using System.Management;
using BrightTime.Models;

namespace BrightTime.Brightness;

public class WmiBrightnessProvider : IBrightnessProvider
{
    private readonly Services.LogService _log;
    private bool _available = true;

    public string Name => "WMI";

    public WmiBrightnessProvider(Services.LogService log)
    {
        _log = log;
    }

    public bool IsAvailable() => _available;

    public BrightnessStatus GetBrightness()
    {
        var st = new BrightnessStatus { Success = false, ProviderName = Name };
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "root\\WMI",
                "SELECT * FROM WmiMonitorBrightness");
            foreach (ManagementObject item in searcher.Get())
            {
                st.CurrentBrightness = Convert.ToInt32(item["CurrentBrightness"]);
                st.MaxBrightness = 100;
                st.Success = true;
                break;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"WMI get: {ex.Message}");
            _available = false;
            st.Error = ex.Message;
        }
        return st;
    }

    public bool SetBrightness(int brightness)
    {
        try
        {
            brightness = Math.Clamp(brightness, 0, 100);
            using var searcher = new ManagementObjectSearcher(
                "root\\WMI",
                "SELECT * FROM WmiMonitorBrightnessMethods");
            bool success = false;
            foreach (ManagementObject method in searcher.Get())
            {
                method.InvokeMethod("WmiSetBrightness", new object[] { 1, brightness });
                success = true;
            }
            if (success) { _log.Info($"WMI set {brightness}"); return true; }
            _log.Warn("WMI: no methods found");
            return false;
        }
        catch (Exception ex)
        {
            _log.Error($"WMI set: {ex.Message}");
            _available = false;
            return false;
        }
    }
}
