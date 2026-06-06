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
        Size = new Size(480, 620);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += SettingsForm_FormClosing;

        BuildUI();
        LoadSettings();

        log.Info("SettingsForm opened");
    }

    private void BuildUI()
    {
        var y = 12;

        lblCurrent = CreateLabel($"Current brightness: --", y); y += 24;
        lblTarget = CreateLabel($"Target brightness: --", y); y += 24;
        lblMethod = CreateLabel($"Active method: --", y); y += 24;
        lblAutoMode = CreateLabel($"Automatic mode: --", y); y += 30;

        var grp = new GroupBox { Text = "Controls", Bounds = new Rectangle(12, y, 440, 200) };
        var gy = 20;

        chkAuto = new CheckBox { Text = "Enable automatic brightness", Bounds = new Rectangle(8, gy, 300, 24) };
        chkAuto.CheckedChanged += (_, _) => { _settings.AutomaticEnabled = chkAuto.Checked; UpdateAutoLabel(); };
        grp.Controls.Add(chkAuto);
        gy += 28;

        var lblTrk = new Label { Text = "Manual brightness:", Bounds = new Rectangle(8, gy, 200, 20) };
        grp.Controls.Add(lblTrk);
        gy += 22;

        lblBrightnessVal = new Label { Text = "100", Bounds = new Rectangle(380, gy, 40, 24), TextAlign = ContentAlignment.MiddleRight };
        trackBrightness = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, Bounds = new Rectangle(8, gy, 370, 40), TickFrequency = 10 };
        trackBrightness.ValueChanged += (_, _) => lblBrightnessVal.Text = trackBrightness.Value.ToString();
        grp.Controls.Add(lblBrightnessVal);
        grp.Controls.Add(trackBrightness);
        gy += 44;

        btnApply = new Button { Text = "Apply Manual Brightness", Bounds = new Rectangle(8, gy, 180, 28) };
        btnApply.Click += BtnApply_Click;
        grp.Controls.Add(btnApply);
        gy += 34;

        chkSmooth = new CheckBox { Text = "Smooth transition", Bounds = new Rectangle(8, gy, 200, 24) };
        grp.Controls.Add(chkSmooth);
        gy += 28;

        chkOverlay = new CheckBox { Text = "Use overlay fallback", Bounds = new Rectangle(8, gy, 200, 24) };
        grp.Controls.Add(chkOverlay);
        gy += 28;

        chkRestore = new CheckBox { Text = "Restore brightness on exit", Bounds = new Rectangle(8, gy, 220, 24) };
        grp.Controls.Add(chkRestore);
        gy += 28;

        chkStartup = new CheckBox { Text = "Start with Windows", Bounds = new Rectangle(8, gy, 200, 24) };
        chkStartup.CheckedChanged += ChkStartup_Changed;
        grp.Controls.Add(chkStartup);

        Controls.Add(grp);
        y += grp.Height + 8;

        var grp2 = new GroupBox { Text = "Schedule", Bounds = new Rectangle(12, y, 440, 200) };
        scheduleList = new ListBox { Bounds = new Rectangle(8, 20, 420, 100), DisplayMember = "Display" };
        _scheduleSource.DataSource = new List<SchedulePoint>();
        scheduleList.DataSource = _scheduleSource;
        grp2.Controls.Add(scheduleList);

        btnAdd = new Button { Text = "Add Point", Bounds = new Rectangle(8, 128, 90, 26) };
        btnAdd.Click += BtnAdd_Click;
        grp2.Controls.Add(btnAdd);

        btnRemove = new Button { Text = "Remove", Bounds = new Rectangle(104, 128, 80, 26) };
        btnRemove.Click += BtnRemove_Click;
        grp2.Controls.Add(btnRemove);

        btnSave = new Button { Text = "Save Schedule", Bounds = new Rectangle(340, 165, 90, 26) };
        btnSave.Click += BtnSave_Click;
        grp2.Controls.Add(btnSave);

        Controls.Add(grp2);
        y += grp2.Height + 8;

        lblStatus = CreateLabel("", y); y += 26;

        btnMinimize = new Button { Text = "Minimize to Tray", Bounds = new Rectangle(12, y, 130, 28) };
        btnMinimize.Click += (_, _) => { _closing = false; Close(); };
        Controls.Add(btnMinimize);

        btnExit = new Button { Text = "Exit", Bounds = new Rectangle(150, y, 80, 28), BackColor = Color.IndianRed };
        btnExit.Click += (_, _) => { _closing = true; Close(); };
        Controls.Add(btnExit);
    }

    private Label CreateLabel(string text, int y)
    {
        var lbl = new Label { Text = text, Bounds = new Rectangle(14, y, 440, 22) };
        Controls.Add(lbl);
        return lbl;
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
        lblCurrent.Text = $"Current brightness: {_brightness.CurrentBrightness}%";
        lblTarget.Text = $"Target brightness: {_brightness.TargetBrightness}%";
        lblMethod.Text = $"Active method: {_brightness.ActiveMethod}";
        lblAutoMode.Text = $"Automatic mode: {(_settings.AutomaticEnabled ? "Enabled" : "Disabled")}";
        lblStatus.Text = _brightness.Status;
    }

    private void UpdateAutoLabel() =>
        lblAutoMode.Text = $"Automatic mode: {(_settings.AutomaticEnabled ? "Enabled" : "Disabled")}";

    private void RefreshSchedule()
    {
        _scheduleSource.DataSource = _settings.Schedule.Select((sp, i) => new ScheduleDisplay
        {
            Index = i,
            Display = $"{sp.Time}  →  {sp.Brightness}%",
            Point = sp
        }).ToList();
        _scheduleSource.ResetBindings(false);
    }

    private class ScheduleDisplay
    {
        public int Index { get; set; }
        public string Display { get; set; } = "";
        public SchedulePoint Point { get; set; } = new();
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
        if (scheduleList.SelectedItem is ScheduleDisplay sd)
        {
            _settings.Schedule.RemoveAt(sd.Index);
            RefreshSchedule();
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _settingsService.Save(_settings);
        MessageBox.Show("Schedule saved.", "BrightTime", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
