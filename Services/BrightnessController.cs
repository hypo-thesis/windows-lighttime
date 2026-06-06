using BrightTime.Models;

namespace BrightTime.Services;

public class BrightnessController : IDisposable
{
    private readonly LogService _log;
    private readonly List<IBrightnessProvider> _providers = new();
    private readonly OverlayBrightnessProvider _overlay;
    private readonly DdcCiBrightnessProvider? _ddcci;
    private readonly AppSettings _settings;

    public int CurrentBrightness { get; private set; } = 100;
    public int TargetBrightness { get; private set; } = 100;
    public string ActiveMethod { get; private set; } = "None";
    public string StatusMessage { get; private set; } = "Ready";

    public BrightnessController(LogService log, AppSettings settings)
    {
        _log = log;
        _settings = settings;
        _overlay = new OverlayBrightnessProvider(log);
        _providers.Add(new WmiBrightnessProvider(log));
        _ddcci = new DdcCiBrightnessProvider(log);
        _providers.Add(_ddcci);
    }

    public async Task<BrightnessStatus> GetBrightnessAsync()
    {
        foreach (var provider in _providers)
        {
            if (!provider.IsAvailable()) continue;
            var status = await provider.GetBrightnessAsync();
            if (status.Success)
            {
                CurrentBrightness = status.CurrentBrightness;
                ActiveMethod = provider.Name;
                return status;
            }
        }
        return new BrightnessStatus { Success = false, Error = "No brightness provider available" };
    }

    public async Task<bool> SetBrightnessAsync(int brightness)
    {
        brightness = Math.Clamp(brightness, 0, 100);
        TargetBrightness = brightness;
        _log.Info($"Setting brightness to {brightness}");

        foreach (var provider in _providers)
        {
            if (!provider.IsAvailable()) continue;
            var success = await provider.SetBrightnessAsync(brightness);
            if (success)
            {
                ActiveMethod = provider.Name;
                CurrentBrightness = brightness;
                StatusMessage = $"Brightness {brightness}% via {provider.Name}";
                _log.Info($"Brightness set to {brightness}% via {provider.Name}");
                return true;
            }
        }

        if (_settings.UseOverlayFallback)
        {
            _log.Warn("Hardware brightness failed, trying overlay fallback");
            var overlaySuccess = await _overlay.SetBrightnessAsync(brightness);
            if (overlaySuccess)
            {
                ActiveMethod = _overlay.Name;
                CurrentBrightness = brightness;
                StatusMessage = $"Overlay dimming active ({brightness}%)";
                return true;
            }
        }

        StatusMessage = "Failed to set brightness";
        _log.Error("All brightness providers failed");
        return false;
    }

    public async Task SmoothTransitionAsync(int fromBrightness, int toBrightness, int steps = 10, int delayMs = 200)
    {
        if (fromBrightness == toBrightness) return;
        for (int i = 1; i <= steps; i++)
        {
            var mid = fromBrightness + (toBrightness - fromBrightness) * i / steps;
            await SetBrightnessAsync(mid);
            await Task.Delay(delayMs);
        }
    }

    public async Task<int> RestorePreviousBrightnessAsync()
    {
        if (_settings.PreviousBrightness.HasValue)
        {
            var prev = _settings.PreviousBrightness.Value;
            await SetBrightnessAsync(prev);
            return prev;
        }
        return 100;
    }

    public void Dispose()
    {
        _overlay.CloseAll();
        _ddcci?.Dispose();
    }
}
