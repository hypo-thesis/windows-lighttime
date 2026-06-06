using BrightTime.Models;

namespace BrightTime.Services;

public class BrightnessController : IDisposable
{
    private readonly LogService _log;
    private readonly AppSettings _settings;
    private readonly WmiBrightnessProvider _wmi;
    private DdcCiBrightnessProvider? _ddcci;
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
        _overlay = new OverlayBrightnessProvider(log);
    }

    public bool SetBrightness(int brightness, bool allowOverlay = true)
    {
        brightness = Math.Clamp(brightness, 0, 100);
        TargetBrightness = brightness;

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

        _ddcci ??= new DdcCiBrightnessProvider(_log);
        if (_ddcci.IsAvailable())
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

        if (allowOverlay && _settings.UseOverlayFallback && !_overlay.IsHiddenForSettings)
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
    }
}
