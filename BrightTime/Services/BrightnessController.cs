using BrightTime.Brightness;
using BrightTime.Models;

namespace BrightTime.Services;

public class BrightnessController : IDisposable
{
    private readonly LogService _log;
    private readonly AppSettings _settings;
    private readonly List<IBrightnessProvider> _providers = new();
    private readonly OverlayBrightnessProvider _overlay;
    private readonly DdcCiBrightnessProvider? _ddcci;
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
        _overlay = new OverlayBrightnessProvider(log);
        _ddcci = new DdcCiBrightnessProvider(log);
        _providers.Add(new WmiBrightnessProvider(log));
        _providers.Add(_ddcci);
    }

    public bool SetBrightness(int brightness)
    {
        brightness = Math.Clamp(brightness, 0, 100);
        TargetBrightness = brightness;
        _log.Info($"Setting brightness to {brightness}");

        if (_overlayActive)
        {
            _overlay.HideAll();
            _overlayActive = false;
        }

        foreach (var p in _providers)
        {
            if (!p.IsAvailable()) continue;
            if (p.SetBrightness(brightness))
            {
                ActiveMethod = p.Name;
                CurrentBrightness = brightness;
                Status = $"Brightness {brightness}% via {p.Name}";
                return true;
            }
        }

        if (_settings.UseOverlayFallback && !_overlay.IsHiddenForSettings)
        {
            _log.Warn("Hardware failed, trying overlay");
            if (_overlay.SetBrightness(brightness))
            {
                ActiveMethod = _overlay.Name;
                CurrentBrightness = brightness;
                _overlayActive = true;
                Status = $"Overlay dimming ({brightness}%)";
                return true;
            }
        }

        Status = "Failed to set brightness";
        _log.Error("All providers failed");
        return false;
    }

    public void SetBrightnessDirect(int brightness)
    {
        SetBrightness(brightness);
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
