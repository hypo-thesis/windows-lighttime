using System.Runtime.InteropServices;
using BrightTime.Models;
using BrightTime.Native;

namespace BrightTime.Brightness;

public class DdcCiBrightnessProvider : IBrightnessProvider, IDisposable
{
    private readonly Services.LogService _log;
    private bool _available = true;
    private readonly List<MonInfo> _mons = new();

    private class MonInfo
    {
        public IntPtr HPhysical;
        public string Desc = "";
        public uint Min, Max;
    }

    public string Name => "DDC/CI";

    public DdcCiBrightnessProvider(Services.LogService log)
    {
        _log = log;
        Enumerate();
    }

    public bool IsAvailable() => _available && _mons.Count > 0;

    private void Enumerate()
    {
        _mons.Clear();
        try
        {
            DdcCiNative.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (hm, hdc, rect, data) =>
                {
                    if (!DdcCiNative.GetNumberOfPhysicalMonitorsFromHMONITOR(hm, out uint cnt) || cnt == 0)
                        return true;
                    var phys = new DdcCiNative.PHYSICAL_MONITOR[cnt];
                    if (DdcCiNative.GetPhysicalMonitorsFromHMONITOR(hm, cnt, phys))
                    {
                        foreach (var p in phys)
                        {
                            if (p.hPhysicalMonitor != IntPtr.Zero &&
                                DdcCiNative.GetMonitorBrightness(p.hPhysicalMonitor, out uint mn, out _, out uint mx))
                            {
                                _mons.Add(new MonInfo
                                {
                                    HPhysical = p.hPhysicalMonitor,
                                    Desc = p.szPhysicalMonitorDescription,
                                    Min = mn, Max = mx
                                });
                                _log.Info($"DDC/CI: {p.szPhysicalMonitorDescription} min={mn} max={mx}");
                            }
                            else
                            {
                                DdcCiNative.DestroyPhysicalMonitor(p.hPhysicalMonitor);
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.Error($"DDC/CI enum: {ex.Message}");
            _available = false;
        }
        if (_mons.Count == 0) _log.Warn("DDC/CI: no compatible monitors");
    }

    public BrightnessStatus GetBrightness()
    {
        var st = new BrightnessStatus { Success = false, ProviderName = Name };
        if (!IsAvailable()) { st.Error = "unavailable"; return st; }
        try
        {
            var m = _mons[0];
            if (DdcCiNative.GetMonitorBrightness(m.HPhysical, out uint mn, out uint cur, out uint mx))
            {
                st.CurrentBrightness = ScaleToPercent(cur, mn, mx);
                st.MinBrightness = 0;
                st.MaxBrightness = 100;
                st.Success = true;
            }
        }
        catch (Exception ex) { st.Error = ex.Message; }
        return st;
    }

    public bool SetBrightness(int brightness)
    {
        if (!IsAvailable()) return false;
        var ok = true;
        foreach (var m in _mons)
        {
            // Map app percentage (0-100) to monitor hardware range (min-max)
            // actual = min + ((max - min) * brightness / 100)
            uint actual = m.Min + (uint)(brightness * (m.Max - m.Min) / 100);
            if (!DdcCiNative.SetMonitorBrightness(m.HPhysical, actual))
            {
                _log.Error($"DDC/CI: set failed on {m.Desc}");
                ok = false;
            }
            else
                _log.Info($"DDC/CI: {m.Desc} set {actual} (target {brightness}%, range {m.Min}-{m.Max})");
        }
        return ok;
    }

    private static int ScaleToPercent(uint v, uint mn, uint mx) =>
        (int)((v - mn) * 100 / (mx - mn));

    public void Dispose()
    {
        foreach (var m in _mons)
            if (m.HPhysical != IntPtr.Zero)
                DdcCiNative.DestroyPhysicalMonitor(m.HPhysical);
        _mons.Clear();
    }
}
