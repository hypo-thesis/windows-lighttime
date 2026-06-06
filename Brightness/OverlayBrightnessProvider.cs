using BrightTime.Models;
using BrightTime.Native;

namespace BrightTime.Brightness;

public class OverlayBrightnessProvider : IBrightnessProvider, IDisposable
{
    private readonly Services.LogService _log;
    private readonly List<OverlayForm> _overlays = new();
    private int _current = 100;

    public string Name => "Overlay";

    public OverlayBrightnessProvider(Services.LogService log)
    {
        _log = log;
    }

    public bool IsAvailable() => true;

    public BrightnessStatus GetBrightness() =>
        new() { CurrentBrightness = _current, MaxBrightness = 100, ProviderName = Name, Success = true };

    public bool SetBrightness(int brightness)
    {
        brightness = Math.Clamp(brightness, 0, 100);
        _current = brightness;

        if (brightness >= 100) { HideAll(); return true; }

        try
        {
            EnsureOverlays();
            var dim = (100.0 - brightness) / 100.0;
            foreach (var o in _overlays) o.SetOpacity(dim);
            _log.Info($"Overlay dim {100 - brightness}%");
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Overlay: {ex.Message}");
            return false;
        }
    }

    private void EnsureOverlays()
    {
        if (_overlays.Count > 0) return;
        foreach (var s in Screen.AllScreens)
        {
            var f = new OverlayForm(s.Bounds);
            f.Show();
            _overlays.Add(f);
        }
    }

    public void HideAll()
    {
        foreach (var o in _overlays) { o.Close(); o.Dispose(); }
        _overlays.Clear();
    }

    public void Dispose() => HideAll();

    private class OverlayForm : Form
    {
        public OverlayForm(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            BackColor = Color.Black;
            Opacity = 0;
            var hwnd = Handle;
            var ex = OverlayNative.GetWindowLong(hwnd, OverlayNative.GWL_EXSTYLE);
            OverlayNative.SetWindowLong(hwnd, OverlayNative.GWL_EXSTYLE,
                ex | OverlayNative.WS_EX_TRANSPARENT | OverlayNative.WS_EX_LAYERED |
                OverlayNative.WS_EX_TOOLWINDOW | OverlayNative.WS_EX_NOACTIVATE);
        }

        public void SetOpacity(double o) => Opacity = o;
    }
}
