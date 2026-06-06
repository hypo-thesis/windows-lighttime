using BrightTime.Models;

namespace BrightTime.Services;

public interface IBrightnessProvider
{
    string Name { get; }
    bool IsAvailable();
    BrightnessStatus GetBrightness();
    bool SetBrightness(int brightness);
}
