using BrightTime.Models;

namespace BrightTime.Brightness;

public interface IBrightnessProvider
{
    string Name { get; }
    bool IsAvailable();
    BrightnessStatus GetBrightness();
    bool SetBrightness(int brightness);
}
