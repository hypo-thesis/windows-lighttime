using BrightTime.Services;

namespace BrightTime;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var single = new SingleInstanceService();
        if (!single.IsFirst) return;

        ApplicationConfiguration.Initialize();

        var log = new LogService();
        var settingsService = new SettingsService(log);
        var settings = settingsService.Load();
        var startup = new StartupService();

        if (settings.StartWithWindows) startup.Enable();
        else startup.Disable();

        var scheduleService = new ScheduleService(settings);
        var brightness = new BrightnessController(log, settings);

        Application.Run(new TrayAppContext(settings, settingsService, brightness,
            scheduleService, startup, log));
    }
}
