using BrightTime.Models;
using BrightTime.Native;

namespace BrightTime.Brightness;

public class DdcCiBrightnessProvider : IBrightnessProvider
{
    private readonly Services.LogService _log;
    private bool _available = true;

    public string Name => "DDC/CI";

    public DdcCiBrightnessProvider(Services.LogService log)
    {
        _log = log;
    }

    public bool IsAvailable() => _available;

    public BrightnessStatus GetBrightness()
    {
        var st = new BrightnessStatus { Success = false, ProviderName = Name };
        if (!_available) { st.Error = "unavailable"; return st; }
        try
        {
            var mons = new List<MonInfo>();
            Enumerate(mons);
            if (mons.Count > 0)
            {
                var m = mons[0];
                st.CurrentBrightness = ScaleToPercent(m.Cur, m.Min, m.Max);
                st.MinBrightness = 0;
                st.MaxBrightness = 100;
                st.Success = true;
            }
            DestroyAll(mons);
        }
        catch (Exception ex) { st.Error = ex.Message; }
        return st;
    }

    public bool SetBrightness(int brightness)
    {
        if (!_available) return false;
        var mons = new List<MonInfo>();
        Enumerate(mons);
        if (mons.Count == 0) { _available = false; return false; }

        var ok = true;
        foreach (var m in mons)
        {
            uint actual = m.Min + (uint)(brightness * (m.Max - m.Min) / 100);
            if (!DdcCiNative.SetMonitorBrightness(m.HPhysical, actual))
            {
                _log.Error($"DDC/CI: set failed on {m.Desc}");
                ok = false;
            }
            else
                _log.Info($"DDC/CI: {m.Desc} set {actual} (target {brightness}%)");
        }
        DestroyAll(mons);
        return ok;
    }

    private void Enumerate(List<MonInfo> mons)
    {
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
                                DdcCiNative.GetMonitorBrightness(p.hPhysicalMonitor, out uint mn, out uint cur, out uint mx))
                            {
                                mons.Add(new MonInfo
                                {
                                    HPhysical = p.hPhysicalMonitor,
                                    Desc = p.szPhysicalMonitorDescription,
                                    Min = mn, Max = mx, Cur = cur
                                });
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
    }

    private static void DestroyAll(List<MonInfo> mons)
    {
        foreach (var m in mons)
            if (m.HPhysical != IntPtr.Zero)
                DdcCiNative.DestroyPhysicalMonitor(m.HPhysical);
    }

    private static int ScaleToPercent(uint v, uint mn, uint mx) =>
        (int)((v - mn) * 100 / (mx - mn));

    private class MonInfo
    {
        public IntPtr HPhysical;
        public string Desc = "";
        public uint Min, Max, Cur;
    }
}
