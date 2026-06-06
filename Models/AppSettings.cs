namespace BrightTime.Models;

public class AppSettings
{
    public bool AutomaticEnabled { get; set; } = true;
    public bool SmoothTransitionEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool UseOverlayFallback { get; set; } = true;
    public bool RestoreBrightnessOnExit { get; set; } = true;
    public int ManualOverrideMinutes { get; set; } = 30;
    public List<SchedulePoint> Schedule { get; set; } = new()
    {
        new() { Time = "07:00", Brightness = 100 },
        new() { Time = "12:00", Brightness = 90 },
        new() { Time = "18:00", Brightness = 70 },
        new() { Time = "21:00", Brightness = 45 },
        new() { Time = "23:30", Brightness = 25 },
    };
    public int LastManualBrightness { get; set; } = 100;
    public DateTime? ManualOverrideUntil { get; set; }
    public int? PreviousBrightness { get; set; }
}
