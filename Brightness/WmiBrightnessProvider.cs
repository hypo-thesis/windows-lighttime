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
            using var s = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\wmi"),
                new ObjectQuery("SELECT * FROM WmiMonitorBrightness"));
            foreach (var o in s.Get())
            {
                st.CurrentBrightness = Convert.ToInt32(o["CurrentBrightness"]);
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
            using var s = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\wmi"),
                new ObjectQuery("SELECT * FROM WmiMonitorBrightnessMethods"));
            var ok = false;
            foreach (ManagementObject o in s.Get())
            {
                using (o)
                {
                    var p = o.GetMethodParameters("WmiSetBrightness");
                    p["Brightness"] = brightness;
                    p["Timeout"] = 0;
                    if (o.InvokeMethod("WmiSetBrightness", p, null) != null)
                        ok = true;
                }
            }
            if (ok) { _log.Info($"WMI set {brightness}"); return true; }
            _log.Warn("WMI: no methods");
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
