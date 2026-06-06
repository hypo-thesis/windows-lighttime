using BrightTimeTray.Services;

namespace BrightTimeTray;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var single = new SingleInstanceService();
        if (!single.IsFirst) return;

        ApplicationConfiguration.Initialize();

        var log = new BrightTime.Services.LogService();
        var settingsService = new BrightTime.Services.SettingsService(log);
        var settings = settingsService.Load();
        var startup = new StartupService();

        if (settings.StartWithWindows) startup.Enable();
        else startup.Disable();

        var scheduleService = new BrightTime.Services.ScheduleService(settings);
        var brightness = new BrightTime.Services.BrightnessController(log, settings);

        Application.Run(new TrayAppContext(settings, settingsService, brightness,
            scheduleService, startup, log));
    }
}
