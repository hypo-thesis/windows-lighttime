using BrightTime.Models;

namespace BrightTime.Services;

public class ScheduleService
{
    private readonly AppSettings _settings;

    public ScheduleService(AppSettings settings)
    {
        _settings = settings;
    }

    public int GetTargetBrightness()
    {
        var s = _settings.Schedule;
        if (s == null || s.Count == 0) return 100;

        var now = DateTime.Now.TimeOfDay;
        var pts = s.Select(p =>
        {
            var parts = p.Time.Split(':');
            return new
            {
                Time = new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), 0),
                p.Brightness
            };
        }).OrderBy(x => x.Time).ToList();

        if (pts.Count == 1) return pts[0].Brightness;

        var first = pts[0];
        var last = pts[^1];

        if (now >= first.Time && now < last.Time)
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (now >= pts[i].Time && now < pts[i + 1].Time)
                {
                    var dur = (pts[i + 1].Time - pts[i].Time).TotalMinutes;
                    if (dur <= 0) return pts[i].Brightness;
                    var el = (now - pts[i].Time).TotalMinutes;
                    return (int)Math.Round(pts[i].Brightness + (pts[i + 1].Brightness - pts[i].Brightness) * el / dur);
                }
            }
        }

        var over = now < first.Time ? now.Add(TimeSpan.FromDays(1)) : now;
        var total = (first.Time.Add(TimeSpan.FromDays(1)) - last.Time).TotalMinutes;
        if (total <= 0) return last.Brightness;
        var elapsed = (over - last.Time).TotalMinutes;
        return (int)Math.Round(last.Brightness + (first.Brightness - last.Brightness) * elapsed / total);
    }
}
