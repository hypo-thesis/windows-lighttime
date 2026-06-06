using BrightTime.Forms;
using BrightTime.Models;
using BrightTime.Services;

namespace BrightTime;

public class TrayAppContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly BrightnessController _brightness;
    private readonly ScheduleService _scheduleService;
    private readonly StartupService _startupService;
    private readonly LogService _log;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer;
    private SettingsForm? _settingsForm;

    public TrayAppContext(AppSettings settings, SettingsService ss, BrightnessController bc,
        ScheduleService sc, StartupService su, LogService log)
    {
        _settings = settings;
        _settingsService = ss;
        _brightness = bc;
        _scheduleService = sc;
        _startupService = su;
        _log = log;

        _tray = new NotifyIcon
        {
            Icon = MakeIcon(),
            Text = "BrightTime",
            ContextMenuStrip = BuildMenu(),
            Visible = true
        };
        _tray.DoubleClick += (_, _) => ShowForm();

        ApplyStartupBrightness();

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 300000;
        _timer.Tick += (_, _) => TimerTick();
        _timer.Start();
    }

    private void ApplyStartupBrightness()
    {
        try
        {
            if (!_settings.AutomaticEnabled) return;
            if (_settings.ManualOverrideUntil.HasValue && DateTime.Now < _settings.ManualOverrideUntil.Value)
                return;

            var target = _scheduleService.GetTargetBrightness();
            _brightness.SetBrightness(target);
        }
        catch { }
    }

    private ContextMenuStrip BuildMenu()
    {
        var m = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("Show");
        showItem.Click += (_, _) => ShowForm();
        m.Items.Add(showItem);

        var autoItem = new ToolStripMenuItem(
            _settings.AutomaticEnabled ? "Disable Automatic Brightness" : "Enable Automatic Brightness");
        autoItem.Click += (_, _) =>
        {
            _settings.AutomaticEnabled = !_settings.AutomaticEnabled;
            autoItem.Text = _settings.AutomaticEnabled ? "Disable Automatic Brightness" : "Enable Automatic Brightness";
            if (_settings.AutomaticEnabled) _settings.ManualOverrideUntil = null;
            _settingsService.Save(_settings);
        };
        m.Items.Add(autoItem);
        m.Items.Add(new ToolStripSeparator());

        AddBrightnessItem(m, "Set Brightness 25%", 25);
        AddBrightnessItem(m, "Set Brightness 50%", 50);
        AddBrightnessItem(m, "Set Brightness 75%", 75);
        AddBrightnessItem(m, "Set Brightness 100%", 100);
        m.Items.Add(new ToolStripSeparator());

        var restoreItem = new ToolStripMenuItem("Restore Previous Brightness");
        restoreItem.Click += (_, _) => _brightness.RestorePrevious();
        m.Items.Add(restoreItem);

        var startupItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = _startupService.IsEnabled()
        };
        startupItem.Click += (_, _) =>
        {
            startupItem.Checked = !startupItem.Checked;
            if (startupItem.Checked) { _startupService.Enable(); _settings.StartWithWindows = true; }
            else { _startupService.Disable(); _settings.StartWithWindows = false; }
            _settingsService.Save(_settings);
        };
        m.Items.Add(startupItem);
        m.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();
        m.Items.Add(exitItem);

        return m;
    }

    private void AddBrightnessItem(ContextMenuStrip m, string text, int val)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) =>
        {
            _brightness.SetBrightness(val);
            _settings.LastManualBrightness = val;
            _settings.ManualOverrideUntil = DateTime.Now.AddMinutes(_settings.ManualOverrideMinutes);
            _settingsService.Save(_settings);
        };
        m.Items.Add(item);
    }

    private void ShowForm()
    {
        _brightness.HideOverlays();

        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_settings, _settingsService, _brightness,
                _scheduleService, _startupService, _log);
            _settingsForm.ExitRequested += ExitApp;
            _settingsForm.FormClosed += (_, _) =>
            {
                _settingsForm = null;
                _brightness.ShowOverlays();
            };
        }
        _settingsForm.Show();
        _settingsForm.Activate();
        _settingsForm.UpdateStatus();
    }

    private void TimerTick()
    {
        try
        {
            if (!_settings.AutomaticEnabled) return;
            if (_settings.ManualOverrideUntil.HasValue && DateTime.Now < _settings.ManualOverrideUntil.Value)
                return;

            _settings.ManualOverrideUntil = null;
            var target = _scheduleService.GetTargetBrightness();

            if (_brightness.CurrentBrightness != target)
            {
                _brightness.SetBrightness(target);
                if (_settingsForm != null && !_settingsForm.IsDisposed && _settingsForm.Visible)
                    _settingsForm.UpdateStatus();
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Timer: {ex.Message}");
        }
    }

    private void ExitApp()
    {
        _timer.Dispose();

        if (_settings.RestoreBrightnessOnExit)
            _brightness.RestorePrevious();

        _settingsService.Save(_settings);

        _settingsForm?.Close();
        _settingsForm?.Dispose();

        _brightness.Dispose();
        _tray.Visible = false;
        _tray.Dispose();

        ExitThread();
    }

    private static Icon MakeIcon()
    {
        using var bmp = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.FillEllipse(Brushes.Gold, 1, 1, 14, 14);
        g.DrawEllipse(Pens.DarkOrange, 1, 1, 14, 14);
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _brightness.Dispose();
            _tray.Dispose();
        }
        base.Dispose(disposing);
    }
}
