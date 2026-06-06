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

        if (_settings.UseOverlayFallback)
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

    public void SmoothTransition(int from, int to, int steps = 10, int delayMs = 200)
    {
        if (from == to) return;
        for (int i = 1; i <= steps; i++)
        {
            var mid = from + (to - from) * i / steps;
            SetBrightness(mid);
            Thread.Sleep(delayMs);
        }
    }

    public void RestorePrevious()
    {
        if (_settings.PreviousBrightness.HasValue)
            SetBrightness(_settings.PreviousBrightness.Value);
    }

    public void Dispose()
    {
        _overlay.Dispose();
        _ddcci?.Dispose();
    }
}
