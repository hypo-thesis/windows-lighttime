using BrightTime.Brightness;
using BrightTime.Models;

namespace BrightTime.Services;

public class BrightnessController : IDisposable
{
    private readonly LogService _log;
    private readonly AppSettings _settings;
    private readonly WmiBrightnessProvider _wmi;
    private readonly DdcCiBrightnessProvider? _ddcci;
    private readonly OverlayBrightnessProvider _overlay;
    private bool _overlayActive;

    public int CurrentBrightness { get; private set; } = 100;
    public int TargetBrightness { get; private set; } = 100;
    public string ActiveMethod { get; private set; } = "None";
    public string Status { get; private set; } = "Ready";
    public bool IsOverlayActive => _overlayActive;

    public BrightnessController(LogService log, AppSettings settings)
    {
        _log = log;
        _settings = settings;
        _wmi = new WmiBrightnessProvider(log);
        _ddcci = new DdcCiBrightnessProvider(log);
        _overlay = new OverlayBrightnessProvider(log);
    }

    public bool SetBrightness(int brightness)
    {
        brightness = Math.Clamp(brightness, 0, 100);
        TargetBrightness = brightness;
        _log.Info($"Setting brightness to {brightness}");

        // 1. Try WMI (internal/laptop display)
        if (_wmi.IsAvailable())
        {
            if (_wmi.SetBrightness(brightness))
            {
                _overlay.DisposeOverlays();
                _overlayActive = false;
                ActiveMethod = _wmi.Name;
                CurrentBrightness = brightness;
                Status = $"WMI hardware brightness ({brightness}%)";
                return true;
            }
        }

        // 2. Try DDC/CI (external monitors)
        if (_ddcci != null && _ddcci.IsAvailable())
        {
            if (_ddcci.SetBrightness(brightness))
            {
                _overlay.DisposeOverlays();
                _overlayActive = false;
                ActiveMethod = "DDC-CI";
                CurrentBrightness = brightness;
                Status = $"DDC-CI hardware brightness ({brightness}%)";
                return true;
            }
        }

        // 3. Overlay fallback – visual dimming only
        if (_settings.UseOverlayFallback && !_overlay.IsHiddenForSettings)
        {
            _log.Warn("Hardware failed, trying overlay fallback");
            if (_overlay.SetBrightness(brightness))
            {
                ActiveMethod = _overlay.Name;
                CurrentBrightness = brightness;
                _overlayActive = true;
                Status = $"Overlay visual dimming fallback ({brightness}%)";
                return true;
            }
        }

        if (_overlayActive)
        {
            _overlay.DisposeOverlays();
            _overlayActive = false;
        }

        Status = "Failed to set brightness";
        _log.Error("All providers failed");
        return false;
    }

    public void RestorePrevious()
    {
        if (_settings.PreviousBrightness.HasValue)
            SetBrightness(_settings.PreviousBrightness.Value);
    }

    public void HideOverlays() => _overlay.HideOverlays();
    public void ShowOverlays() => _overlay.ShowOverlays();
    public void DisposeOverlays() => _overlay.DisposeOverlays();

    public void Dispose()
    {
        _overlay.Dispose();
        _ddcci?.Dispose();
    }
}
