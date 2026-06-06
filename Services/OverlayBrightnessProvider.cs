using System.Windows;
using System.Windows.Interop;
using BrightTime.Models;
using BrightTime.Native;

namespace BrightTime.Services;

public class OverlayBrightnessProvider : IBrightnessProvider
{
    private readonly LogService _log;
    private readonly List<OverlayWindow> _overlays = new();
    private int _currentBrightness = 100;

    public string Name => "Overlay";

    public OverlayBrightnessProvider(LogService log)
    {
        _log = log;
    }

    public bool IsAvailable() => true;

    public Task<BrightnessStatus> GetBrightnessAsync()
    {
        return Task.FromResult(new BrightnessStatus
        {
            CurrentBrightness = _currentBrightness,
            MinBrightness = 0,
            MaxBrightness = 100,
            ProviderName = Name,
            Success = true
        });
    }

    public async Task<bool> SetBrightnessAsync(int brightness)
    {
        brightness = Math.Clamp(brightness, 0, 100);
        _currentBrightness = brightness;

        if (brightness >= 100)
        {
            HideAllOverlays();
            return true;
        }

        try
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                EnsureOverlays();
                var dimLevel = (100.0 - brightness) / 100.0;
                foreach (var overlay in _overlays)
                {
                    overlay.UpdateOpacity(dimLevel);
                }
            });
            _log.Info($"Overlay dimming set to {(100 - brightness)}%");
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Overlay failed: {ex.Message}");
            return false;
        }
    }

    private void EnsureOverlays()
    {
        if (_overlays.Count > 0) return;

        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            var window = new OverlayWindow(screen.Bounds);
            window.Show();
            _overlays.Add(window);
        }
    }

    private void HideAllOverlays()
    {
        foreach (var overlay in _overlays)
        {
            overlay.Close();
        }
        _overlays.Clear();
    }

    public void CloseAll()
    {
        HideAllOverlays();
    }

    private class OverlayWindow : Window
    {
        public OverlayWindow(System.Drawing.Rectangle bounds)
        {
            Title = "BrightTimeOverlay";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(1, 0, 0, 0));
            WindowState = WindowState.Normal;
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
            Visibility = Visibility.Visible;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = MonitorNativeMethods.GetWindowLong(hwnd, MonitorNativeMethods.GWL_EXSTYLE);
            MonitorNativeMethods.SetWindowLong(hwnd, MonitorNativeMethods.GWL_EXSTYLE,
                exStyle |
                MonitorNativeMethods.WS_EX_TRANSPARENT |
                MonitorNativeMethods.WS_EX_LAYERED |
                MonitorNativeMethods.WS_EX_TOOLWINDOW |
                MonitorNativeMethods.WS_EX_NOACTIVATE);
        }

        public void UpdateOpacity(double dimLevel)
        {
            Opacity = dimLevel;
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(1, 0, 0, 0));
        }
    }
}
