using BrightTime.Models;
using BrightTime.Services;

namespace BrightTime.Forms;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly BrightnessController _brightness;
    private readonly ScheduleService _scheduleService;
    private readonly StartupService _startupService;
    private readonly LogService _log;

    private CheckBox chkAuto = null!;
    private TrackBar trackBrightness = null!;
    private Label lblBrightnessVal = null!;
    private Button btnApply = null!;
    private CheckBox chkSmooth = null!;
    private CheckBox chkOverlay = null!;
    private CheckBox chkRestore = null!;
    private CheckBox chkStartup = null!;
    private ListBox scheduleList = null!;
    private Button btnAdd = null!;
    private Button btnRemove = null!;
    private Button btnSave = null!;
    private Button btnMinimize = null!;
    private Button btnExit = null!;
    private Label lblStatus = null!;
    private Label lblCurrent = null!;
    private Label lblTarget = null!;
    private Label lblMethod = null!;
    private Label lblAutoMode = null!;

    private readonly BindingSource _scheduleSource = new();
    private bool _closing;

    public SettingsForm(AppSettings settings, SettingsService ss, BrightnessController bc,
        ScheduleService sc, StartupService su, LogService log)
    {
        _settings = settings;
        _settingsService = ss;
        _brightness = bc;
        _scheduleService = sc;
        _startupService = su;
        _log = log;

        Text = "BrightTime";
        Size = new Size(700, 660);
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += SettingsForm_FormClosing;

        BuildUI();
        LoadSettings();

        log.Info("SettingsForm opened");
    }

    private void BuildUI()
    {
        AutoScroll = true;
        var pad = 14;
        var cw = ClientSize.Width - pad * 2;
        var x = pad;
        var y = 8;

        Controls.Add(new Label
        {
            Text = "BrightTime",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location = new Point(x, y),
            Size = new Size(cw, 28),
        });
        y += 36;

        Controls.Add(lblCurrent = new Label { Text = "Current: --", Location = new Point(x, y), Size = new Size(cw, 20) }); y += 22;
        Controls.Add(lblTarget = new Label { Text = "Target: --", Location = new Point(x, y), Size = new Size(cw, 20) }); y += 22;
        Controls.Add(lblMethod = new Label { Text = "Method: --", Location = new Point(x, y), Size = new Size(cw, 20) }); y += 22;
        Controls.Add(lblAutoMode = new Label { Text = "Mode: --", Location = new Point(x, y), Size = new Size(cw, 20) }); y += 26;

        var g1 = new GroupBox { Text = "Controls", Location = new Point(x, y), Size = new Size(cw, 280) };
        var gy = 20;
        g1.Controls.Add(chkAuto = new CheckBox { Text = "Auto brightness", Location = new Point(10, gy), Size = new Size(300, 24) });
        chkAuto.CheckedChanged += (_, _) => { _settings.AutomaticEnabled = chkAuto.Checked; UpdateAutoLabel(); };
        gy += 30;

        g1.Controls.Add(new Label { Text = "Manual brightness:", Location = new Point(10, gy), Size = new Size(200, 18) });
        gy += 22;

        g1.Controls.Add(lblBrightnessVal = new Label { Text = "100", Location = new Point(cw - 70, gy), Size = new Size(50, 24), TextAlign = ContentAlignment.MiddleRight });
        g1.Controls.Add(trackBrightness = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, Location = new Point(10, gy), Size = new Size(cw - 90, 36), TickFrequency = 10 });
        trackBrightness.ValueChanged += (_, _) => lblBrightnessVal.Text = trackBrightness.Value.ToString();
        gy += 44;

        g1.Controls.Add(btnApply = new Button { Text = "Apply", Location = new Point(10, gy), Size = new Size(180, 28) });
        btnApply.Click += BtnApply_Click;
        gy += 36;

        g1.Controls.Add(chkSmooth = new CheckBox { Text = "Smooth", Location = new Point(10, gy), Size = new Size(200, 24) }); gy += 28;
        g1.Controls.Add(chkOverlay = new CheckBox { Text = "Overlay fallback", Location = new Point(10, gy), Size = new Size(200, 24) }); gy += 28;
        g1.Controls.Add(chkRestore = new CheckBox { Text = "Restore on exit", Location = new Point(10, gy), Size = new Size(220, 24) }); gy += 28;
        g1.Controls.Add(chkStartup = new CheckBox { Text = "Start with Windows", Location = new Point(10, gy), Size = new Size(200, 24) });
        chkStartup.CheckedChanged += ChkStartup_Changed;
        Controls.Add(g1);
        y += 288;

        var g2 = new GroupBox { Text = "Schedule", Location = new Point(x, y), Size = new Size(cw, 260) };
        g2.Controls.Add(scheduleList = new ListBox { Location = new Point(10, 20), Size = new Size(cw - 24, 130) });
        _scheduleSource.DataSource = new List<SchedulePoint>();
        scheduleList.DataSource = _scheduleSource;
        scheduleList.MouseDoubleClick += ScheduleList_DoubleClick;
        g2.Controls.Add(btnAdd = new Button { Text = "Add", Location = new Point(10, 158), Size = new Size(90, 26) });
        btnAdd.Click += BtnAdd_Click;
        g2.Controls.Add(btnRemove = new Button { Text = "Remove", Location = new Point(108, 158), Size = new Size(90, 26) });
        btnRemove.Click += BtnRemove_Click;
        g2.Controls.Add(btnSave = new Button { Text = "Save", Location = new Point(cw - 100, 220), Size = new Size(90, 26) });
        btnSave.Click += BtnSave_Click;
        Controls.Add(g2);
        y += 268;

        Controls.Add(lblStatus = new Label { Text = "", Location = new Point(x, y), Size = new Size(cw, 20), ForeColor = Color.DarkOrange }); y += 26;

        Controls.Add(btnMinimize = new Button { Text = "Minimize to Tray", Location = new Point(x, y), Size = new Size(140, 28) });
        btnMinimize.Click += (_, _) => { _closing = false; Close(); };
        Controls.Add(btnExit = new Button { Text = "Exit", Location = new Point(x + 148, y), Size = new Size(90, 28), BackColor = Color.Crimson, ForeColor = Color.White });
        btnExit.Click += (_, _) => { _closing = true; Close(); };
    }

    private void ScheduleList_DoubleClick(object? sender, MouseEventArgs e)
    {
        var idx = scheduleList.SelectedIndex;
        if (idx >= 0 && idx < _settings.Schedule.Count)
        {
            var pt = _settings.Schedule[idx];
            using var dlg = new ScheduleEditDialog(pt);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshSchedule();
            }
        }
    }

    private void LoadSettings()
    {
        chkAuto.Checked = _settings.AutomaticEnabled;
        trackBrightness.Value = _settings.LastManualBrightness;
        chkSmooth.Checked = _settings.SmoothTransitionEnabled;
        chkOverlay.Checked = _settings.UseOverlayFallback;
        chkRestore.Checked = _settings.RestoreBrightnessOnExit;
        chkStartup.Checked = _startupService.IsEnabled();
        RefreshSchedule();
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        if (IsDisposed) return;
        lblCurrent.Text = $"Current: {_brightness.CurrentBrightness}%";
        lblTarget.Text = $"Target: {_brightness.TargetBrightness}%";
        lblMethod.Text = $"Method: {_brightness.ActiveMethod}";
        lblAutoMode.Text = $"Mode: {(_settings.AutomaticEnabled ? "Auto" : "Manual")}";
        lblStatus.Text = _brightness.Status;
    }

    private void UpdateAutoLabel() =>
        lblAutoMode.Text = $"Mode: {(_settings.AutomaticEnabled ? "Auto" : "Manual")}";

    private void RefreshSchedule()
    {
        _scheduleSource.DataSource = _settings.Schedule.Select((sp, i) => new
        {
            Display = $"{sp.Time}  ->  {sp.Brightness}%"
        }).ToList();
        _scheduleSource.ResetBindings(false);
    }

    private class ScheduleEditDialog : Form
    {
        public ScheduleEditDialog(SchedulePoint pt)
        {
            Text = "Edit Point";
            Size = new Size(280, 160);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;

            new Label { Text = "Time (HH:MM):", Location = new Point(12, 16), Size = new Size(90, 20), Parent = this };
            var tt = new TextBox { Text = pt.Time, Location = new Point(110, 14), Size = new Size(70, 22), Parent = this };
            new Label { Text = "Brightness:", Location = new Point(12, 46), Size = new Size(90, 20), Parent = this };
            var tb = new TextBox { Text = pt.Brightness.ToString(), Location = new Point(110, 44), Size = new Size(50, 22), Parent = this };

            var okb = new Button { Text = "OK", Location = new Point(60, 82), Size = new Size(70, 26), Parent = this };
            okb.Click += (_, _) => { pt.Time = tt.Text; if (int.TryParse(tb.Text, out var v)) pt.Brightness = Math.Clamp(v, 0, 100); DialogResult = DialogResult.OK; Close(); };
            var cb = new Button { Text = "Cancel", Location = new Point(140, 82), Size = new Size(70, 26), Parent = this };
            cb.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        }
    }

    private void BtnApply_Click(object? sender, EventArgs e)
    {
        var b = trackBrightness.Value;
        _settings.LastManualBrightness = b;
        _settings.ManualOverrideUntil = DateTime.Now.AddMinutes(_settings.ManualOverrideMinutes);
        _brightness.SetBrightness(b);
        _settingsService.Save(_settings);
        UpdateStatus();
    }

    private void ChkStartup_Changed(object? sender, EventArgs e)
    {
        if (chkStartup.Checked) { _startupService.Enable(); _settings.StartWithWindows = true; }
        else { _startupService.Disable(); _settings.StartWithWindows = false; }
        _settingsService.Save(_settings);
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        _settings.Schedule.Add(new SchedulePoint { Time = "12:00", Brightness = 50 });
        RefreshSchedule();
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (scheduleList.SelectedItem != null)
        {
            var idx = scheduleList.SelectedIndex;
            if (idx >= 0 && idx < _settings.Schedule.Count)
            {
                _settings.Schedule.RemoveAt(idx);
                RefreshSchedule();
            }
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _settingsService.Save(_settings);
        MessageBox.Show("Saved.", "BrightTime", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_closing)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
