using System.Management;
using BrightTime.Models;

namespace BrightTime.Services;

public class WmiBrightnessProvider : IBrightnessProvider
{
    private readonly LogService _log;
    private bool _available = true;

    public string Name => "WMI";

    public WmiBrightnessProvider(LogService log)
    {
        _log = log;
    }

    public bool IsAvailable() => _available;

    public Task<BrightnessStatus> GetBrightnessAsync()
    {
        var status = new BrightnessStatus { Success = false, ProviderName = Name };
        try
        {
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\wmi"),
                new ObjectQuery("SELECT * FROM WmiMonitorBrightness"));
            foreach (var mo in searcher.Get())
            {
                status.CurrentBrightness = Convert.ToInt32(mo["CurrentBrightness"]);
                status.MinBrightness = 0;
                status.MaxBrightness = 100;
                status.Success = true;
                break;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"WMI get brightness failed: {ex.Message}");
            _available = false;
            status.Error = ex.Message;
        }
        return Task.FromResult(status);
    }

    public Task<bool> SetBrightnessAsync(int brightness)
    {
        try
        {
            brightness = Math.Clamp(brightness, 0, 100);
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\wmi"),
                new ObjectQuery("SELECT * FROM WmiMonitorBrightnessMethods"));
            var set = false;
            foreach (ManagementObject mo in searcher.Get())
            {
                using (mo)
                {
                    var inParams = mo.GetMethodParameters("WmiSetBrightness");
                    inParams["Brightness"] = brightness;
                    inParams["Timeout"] = 0;
                    var result = mo.InvokeMethod("WmiSetBrightness", inParams, null);
                    if (result != null)
                        set = true;
                }
            }
            if (set)
            {
                _log.Info($"WMI set brightness to {brightness}");
                return Task.FromResult(true);
            }
            _log.Warn("WMI: No brightness methods found");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _log.Error($"WMI set brightness failed: {ex.Message}");
            _available = false;
            return Task.FromResult(false);
        }
    }
}
