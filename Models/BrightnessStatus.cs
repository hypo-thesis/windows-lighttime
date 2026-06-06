namespace BrightTime.Models;

public class BrightnessStatus
{
    public int CurrentBrightness { get; set; }
    public int MinBrightness { get; set; }
    public int MaxBrightness { get; set; } = 100;
    public string ProviderName { get; set; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
}
