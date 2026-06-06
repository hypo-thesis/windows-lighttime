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
        log.Info("BrightTime starting");

        var settingsService = new SettingsService(log);
        var settings = settingsService.Load();

        if (settings.StartWithWindows)
            new StartupService().Enable();
        else
            new StartupService().Disable();

        var scheduleService = new ScheduleService(settings);
        var brightness = new BrightnessController(log, settings);
        var startup = new StartupService();

        Application.Run(new TrayAppContext(settings, settingsService, brightness,
            scheduleService, startup, log));

        log.Info("BrightTime exited");
    }
}
