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
        var schedule = _settings.Schedule;
        if (schedule == null || schedule.Count == 0)
            return 100;

        var now = DateTime.Now.TimeOfDay;
        var points = schedule
            .Select(p => new
            {
                Time = new TimeSpan(
                    int.Parse(p.Time.Split(':')[0]),
                    int.Parse(p.Time.Split(':')[1]),
                    0),
                p.Brightness
            })
            .OrderBy(p => p.Time)
            .ToList();

        if (points.Count == 1)
            return points[0].Brightness;

        TimeSpan prevTime, nextTime;
        int prevBrightness, nextBrightness;

        var first = points[0];
        var last = points[^1];

        if (now < first.Time || now >= last.Time)
        {
            prevTime = last.Time;
            prevBrightness = last.Brightness;
            nextTime = first.Time.Add(TimeSpan.FromDays(1));
            nextBrightness = first.Brightness;

            if (now < first.Time)
            {
                now = now.Add(TimeSpan.FromDays(1));
            }
        }
        else
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (now >= points[i].Time && now < points[i + 1].Time)
                {
                    prevTime = points[i].Time;
                    prevBrightness = points[i].Brightness;
                    nextTime = points[i + 1].Time;
                    nextBrightness = points[i + 1].Brightness;
                    break;
                }
            }

            prevTime = last.Time;
            prevBrightness = last.Brightness;
            nextTime = first.Time.Add(TimeSpan.FromDays(1));
            nextBrightness = first.Brightness;
            if (now < first.Time)
                now = now.Add(TimeSpan.FromDays(1));
        }

        var totalDuration = (nextTime - prevTime).TotalMinutes;
        if (totalDuration <= 0) return prevBrightness;

        var elapsed = (now - prevTime).TotalMinutes;
        var ratio = elapsed / totalDuration;

        return (int)Math.Round(prevBrightness + (nextBrightness - prevBrightness) * ratio);
    }
}
