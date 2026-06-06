using BrightTime.Models;
using BrightTime.Native;

namespace BrightTime.Services;

public class DdcCiBrightnessProvider : IBrightnessProvider
{
    private readonly LogService _log;
    private bool _available = true;
    private readonly List<MonitorInfo> _monitors = new();

    private class MonitorInfo
    {
        public IntPtr HMonitor { get; set; }
        public IntPtr HPhysicalMonitor { get; set; }
        public string Description { get; set; } = "";
        public uint MinBrightness { get; set; }
        public uint MaxBrightness { get; set; }
    }

    public string Name => "DDC/CI";

    public DdcCiBrightnessProvider(LogService log)
    {
        _log = log;
        EnumerateMonitors();
    }

    public bool IsAvailable() => _available && _monitors.Count > 0;

    private void EnumerateMonitors()
    {
        _monitors.Clear();
        try
        {
            MonitorNativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (hMonitor, hdc, lprcMonitor, data) =>
                {
                    if (!MonitorNativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count))
                        return true;
                    if (count == 0)
                        return true;

                    var physMonitors = new MonitorNativeMethods.PHYSICAL_MONITOR[count];
                    if (MonitorNativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physMonitors))
                    {
                        foreach (var pm in physMonitors)
                        {
                            if (pm.hPhysicalMonitor != IntPtr.Zero)
                            {
                                var info = new MonitorInfo
                                {
                                    HMonitor = hMonitor,
                                    HPhysicalMonitor = pm.hPhysicalMonitor,
                                    Description = pm.szPhysicalMonitorDescription
                                };

                                if (MonitorNativeMethods.GetMonitorBrightness(
                                    pm.hPhysicalMonitor,
                                    out uint minB, out uint curB, out uint maxB))
                                {
                                    info.MinBrightness = minB;
                                    info.MaxBrightness = maxB;
                                    _monitors.Add(info);
                                    _log.Info($"DDC/CI: Found monitor '{pm.szPhysicalMonitorDescription}' brightness range {minB}-{maxB}");
                                }
                                else
                                {
                                    MonitorNativeMethods.DestroyPhysicalMonitor(pm.hPhysicalMonitor);
                                    _log.Warn($"DDC/CI: Monitor '{pm.szPhysicalMonitorDescription}' does not support brightness");
                                }
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.Error($"DDC/CI enumeration failed: {ex.Message}");
            _available = false;
        }

        if (_monitors.Count == 0)
        {
            _log.Warn("DDC/CI: No monitors with brightness support found");
        }
    }

    public Task<BrightnessStatus> GetBrightnessAsync()
    {
        var status = new BrightnessStatus { Success = false, ProviderName = Name };
        try
        {
            if (!IsAvailable())
            {
                status.Error = "No DDC/CI monitors available";
                return Task.FromResult(status);
            }

            var monitor = _monitors.FirstOrDefault();
            if (monitor == null)
            {
                status.Error = "No monitors";
                return Task.FromResult(status);
            }

            if (MonitorNativeMethods.GetMonitorBrightness(
                monitor.HPhysicalMonitor,
                out uint minB, out uint curB, out uint maxB))
            {
                status.CurrentBrightness = (int)curB;
                status.MinBrightness = (int)minB;
                status.MaxBrightness = (int)maxB;
                status.Success = true;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"DDC/CI get brightness failed: {ex.Message}");
            status.Error = ex.Message;
        }
        return Task.FromResult(status);
    }

    public Task<bool> SetBrightnessAsync(int brightness)
    {
        try
        {
            if (!IsAvailable())
            {
                _log.Warn("DDC/CI: No monitors available to set brightness");
                return Task.FromResult(false);
            }

            var allSucceeded = true;
            foreach (var monitor in _monitors)
            {
                var scaledBrightness = ScaleBrightness(brightness, monitor.MinBrightness, monitor.MaxBrightness);
                if (!MonitorNativeMethods.SetMonitorBrightness(monitor.HPhysicalMonitor, scaledBrightness))
                {
                    _log.Error($"DDC/CI: Failed to set brightness on '{monitor.Description}'");
                    allSucceeded = false;
                }
                else
                {
                    _log.Info($"DDC/CI: Set brightness to {scaledBrightness} ({brightness}%) on '{monitor.Description}'");
                }
            }
            return Task.FromResult(allSucceeded);
        }
        catch (Exception ex)
        {
            _log.Error($"DDC/CI set brightness failed: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    private static uint ScaleBrightness(int percentage, uint min, uint max)
    {
        var range = max - min;
        return min + (uint)(percentage * range / 100);
    }

    public void Dispose()
    {
        foreach (var monitor in _monitors)
        {
            if (monitor.HPhysicalMonitor != IntPtr.Zero)
            {
                MonitorNativeMethods.DestroyPhysicalMonitor(monitor.HPhysicalMonitor);
            }
        }
        _monitors.Clear();
    }
}
