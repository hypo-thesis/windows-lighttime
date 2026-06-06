using BrightTime.Models;

namespace BrightTime.Services;

public interface IBrightnessProvider
{
    string Name { get; }
    bool IsAvailable();
    Task<BrightnessStatus> GetBrightnessAsync();
    Task<bool> SetBrightnessAsync(int brightness);
}
