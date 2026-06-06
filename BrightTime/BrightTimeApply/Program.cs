using BrightTime.Services;

var log = new LogService();
int exitCode = 0;

try
{
    var settingsService = new SettingsService(log);
    var settings = settingsService.Load();
    var scheduleService = new ScheduleService(settings);
    var controller = new BrightnessController(log, settings);

    int target;
    var cmdArgs = Environment.GetCommandLineArgs();

    if (cmdArgs.Length > 1 && cmdArgs[1] == "--brightness" && cmdArgs.Length > 2)
    {
        target = int.TryParse(cmdArgs[2], out var v) ? Math.Clamp(v, 0, 100) : 100;
    }
    else if (cmdArgs.Length > 1 && cmdArgs[1] == "--status")
    {
        var b = controller.QueryCurrentBrightness();
        Console.WriteLine(b >= 0 ? b : "unknown");
        return b >= 0 ? 0 : 1;
    }
    else
    {
        if (!settings.AutomaticEnabled) return 0;
        if (settings.ManualOverrideUntil.HasValue && DateTime.Now < settings.ManualOverrideUntil.Value) return 0;
        target = scheduleService.GetTargetBrightness();
    }

    if (controller.CurrentBrightness != target)
    {
        controller.SetBrightness(target, allowOverlay: false);
    }
}
catch (Exception ex)
{
    log.Error($"Apply: {ex.Message}");
    exitCode = 1;
}

return exitCode;
