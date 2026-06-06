using System.Windows;
using BrightTime.Models;
using BrightTime.Services;

namespace BrightTime;

public partial class App : System.Windows.Application
{
    private SingleInstanceService? _singleInstance;
    private SettingsService? _settingsService;
    private LogService _log = null!;
    private AppSettings _settings = null!;
    private BrightnessController _brightnessController = null!;
    private TrayService _trayService = null!;
    private StartupService _startupService = null!;
    private ScheduleService _scheduleService = null!;
    private System.Timers.Timer? _scheduleTimer;
    private MainWindow? _mainWindow;
    private bool _isFirstLaunch;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.IsFirstInstance)
        {
            _singleInstance.BringExistingToFront();
            _singleInstance.Dispose();
            Shutdown();
            return;
        }

        _log = new LogService();
        _log.Info("BrightTime starting");

        _settingsService = new SettingsService(_log);
        _settings = _settingsService.Load();
        _isFirstLaunch = !System.IO.Directory.Exists(
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BrightTime"));

        _brightnessController = new BrightnessController(_log, _settings);
        _startupService = new StartupService();
        _scheduleService = new ScheduleService(_settings);

        if (_settings.StartWithWindows)
            _startupService.Enable();
        else
            _startupService.Disable();

        _trayService = new TrayService(_settings, _log, _brightnessController);
        _trayService.ShowWindowRequested += OnShowWindow;
        _trayService.ExitRequested += OnExit;
        _trayService.ToggleAutoRequested += OnToggleAuto;
        _trayService.SetBrightnessRequested += OnSetBrightnessFromTray;
        _trayService.RestorePreviousBrightnessRequested += OnRestorePrevious;

        _mainWindow = new MainWindow(_settings, _log, _brightnessController, _scheduleService, _settingsService, _startupService);
        _mainWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            _mainWindow.Hide();
        };
        _mainWindow.ExitRequested += OnExit;

        if (_isFirstLaunch)
        {
            _mainWindow.Show();
        }

        StartScheduleTimer();
        _log.Info("BrightTime started");
    }

    private void StartScheduleTimer()
    {
        _scheduleTimer = new System.Timers.Timer(60000);
        _scheduleTimer.Elapsed += async (_, _) =>
        {
            try
            {
                if (!_settings.AutomaticEnabled)
                    return;

                if (_settings.ManualOverrideUntil.HasValue &&
                    DateTime.Now < _settings.ManualOverrideUntil.Value)
                    return;

                _settings.ManualOverrideUntil = null;

                var target = _scheduleService.GetTargetBrightness();
                var current = _brightnessController.CurrentBrightness;

                if (current != target)
                {
                    _log.Info($"Schedule: target brightness {target} (current: {current})");
                    if (_settings.SmoothTransitionEnabled)
                    {
                        await _brightnessController.SmoothTransitionAsync(current, target, 10, 200);
                    }
                    else
                    {
                        await _brightnessController.SetBrightnessAsync(target);
                    }

                    if (_mainWindow != null && _mainWindow.IsVisible)
                    {
                        _mainWindow.UpdateStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Schedule timer error: {ex.Message}");
            }
        };
        _scheduleTimer.Start();
        _log.Info("Schedule timer started (60s interval)");
    }

    private void OnShowWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_mainWindow == null) return;
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;
            _mainWindow.Focus();
        });
    }

    private void OnToggleAuto(bool enabled)
    {
        _settings!.AutomaticEnabled = enabled;
        if (enabled)
        {
            _settings.ManualOverrideUntil = null;
        }
        _settingsService!.Save(_settings);
        _log!.Info($"Automatic brightness {(enabled ? "enabled" : "disabled")}");
    }

    private async void OnSetBrightnessFromTray(int brightness)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await _brightnessController!.SetBrightnessAsync(brightness);
            _settings!.LastManualBrightness = brightness;
            _settings.ManualOverrideUntil = DateTime.Now.AddMinutes(_settings.ManualOverrideMinutes);
            _settingsService!.Save(_settings);
        });
    }

    private async void OnRestorePrevious()
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await _brightnessController!.RestorePreviousBrightnessAsync();
        });
    }

    private async void OnExit()
    {
        if (_isExiting) return;
        _isExiting = true;

        _log!.Info("BrightTime exiting");

        _scheduleTimer?.Stop();
        _scheduleTimer?.Dispose();

        if (_settings!.RestoreBrightnessOnExit)
        {
            if (_settings.PreviousBrightness.HasValue)
            {
                await _brightnessController!.SetBrightnessAsync(_settings.PreviousBrightness.Value);
            }
        }

        _settingsService!.Save(_settings);
        _trayService!.Dispose();
        _brightnessController!.Dispose();
        _singleInstance!.Dispose();

        _log.Info("BrightTime exited");
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        base.OnExit(e);
    }
}
