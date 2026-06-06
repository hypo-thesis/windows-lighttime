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
        Size = new Size(750, 850);
        MinimumSize = new Size(600, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += SettingsForm_FormClosing;

        BuildUI();
        LoadSettings();

        log.Info("SettingsForm opened");
    }

    private Control Sep(int h) => new Panel { Height = h, Dock = DockStyle.Top };

    private void BuildUI()
    {
        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        body.Padding = new Padding(12, 8, 12, 8);

        var s = new Label { Text = "BrightTime", Font = new Font("Segoe UI", 16f, FontStyle.Bold), Height = 34, Dock = DockStyle.Top };
        body.Controls.Add(s);
        body.Controls.Add(Sep(4));

        body.Controls.Add(lblCurrent = new Label { Text = "Current brightness: --", Height = 24, Dock = DockStyle.Top });
        body.Controls.Add(lblTarget = new Label { Text = "Target brightness: --", Height = 24, Dock = DockStyle.Top });
        body.Controls.Add(lblMethod = new Label { Text = "Active method: --", Height = 24, Dock = DockStyle.Top });
        body.Controls.Add(lblAutoMode = new Label { Text = "Automatic mode: --", Height = 24, Dock = DockStyle.Top });
        body.Controls.Add(Sep(8));

        var grp = new GroupBox { Text = "Controls", Height = 290, Dock = DockStyle.Top };
        grp.Controls.Add(chkAuto = new CheckBox { Text = "Enable automatic brightness", Location = new Point(12, 24), Width = 350, Height = 26 });
        chkAuto.CheckedChanged += (_, _) => { _settings.AutomaticEnabled = chkAuto.Checked; UpdateAutoLabel(); };
        grp.Controls.Add(new Label { Text = "Manual brightness:", Location = new Point(12, 56), Width = 200, Height = 20 });
        grp.Controls.Add(lblBrightnessVal = new Label { Text = "100", Location = new Point(540, 80), Width = 50, Height = 26, TextAlign = ContentAlignment.MiddleRight });
        grp.Controls.Add(trackBrightness = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, Location = new Point(12, 80), Width = 520, Height = 40, TickFrequency = 10 });
        trackBrightness.ValueChanged += (_, _) => lblBrightnessVal.Text = trackBrightness.Value.ToString();
        grp.Controls.Add(btnApply = new Button { Text = "Apply Manual Brightness", Location = new Point(12, 126), Width = 200, Height = 30 });
        btnApply.Click += BtnApply_Click;
        grp.Controls.Add(chkSmooth = new CheckBox { Text = "Smooth transition", Location = new Point(12, 162), Width = 250, Height = 26 });
        grp.Controls.Add(chkOverlay = new CheckBox { Text = "Use overlay fallback", Location = new Point(12, 192), Width = 250, Height = 26 });
        grp.Controls.Add(chkRestore = new CheckBox { Text = "Restore brightness on exit", Location = new Point(12, 222), Width = 280, Height = 26 });
        grp.Controls.Add(chkStartup = new CheckBox { Text = "Start with Windows", Location = new Point(12, 252), Width = 250, Height = 26 });
        chkStartup.CheckedChanged += ChkStartup_Changed;
        body.Controls.Add(grp);
        body.Controls.Add(Sep(8));

        var grp2 = new GroupBox { Text = "Schedule", Height = 280, Dock = DockStyle.Top };
        grp2.Controls.Add(scheduleList = new ListBox { Location = new Point(12, 22), Width = 520, Height = 150 });
        _scheduleSource.DataSource = new List<SchedulePoint>();
        scheduleList.DataSource = _scheduleSource;
        scheduleList.MouseDoubleClick += ScheduleList_DoubleClick;
        grp2.Controls.Add(btnAdd = new Button { Text = "Add Point", Location = new Point(12, 180), Width = 100, Height = 28 });
        btnAdd.Click += BtnAdd_Click;
        grp2.Controls.Add(btnRemove = new Button { Text = "Remove Selected", Location = new Point(120, 180), Width = 130, Height = 28 });
        btnRemove.Click += BtnRemove_Click;
        grp2.Controls.Add(btnSave = new Button { Text = "Save Schedule", Location = new Point(420, 240), Width = 110, Height = 28, Font = new Font(Font, FontStyle.Bold) });
        btnSave.Click += BtnSave_Click;
        body.Controls.Add(grp2);
        body.Controls.Add(Sep(8));

        body.Controls.Add(lblStatus = new Label { Text = "", Height = 24, Dock = DockStyle.Top, ForeColor = Color.DarkOrange });
        body.Controls.Add(Sep(8));

        var btnRow = new Panel { Height = 40, Dock = DockStyle.Top };
        btnMinimize = new Button { Text = "Minimize to Tray", Location = new Point(0, 4), Width = 150, Height = 30 };
        btnMinimize.Click += (_, _) => { _closing = false; Close(); };
        btnRow.Controls.Add(btnMinimize);
        btnExit = new Button { Text = "Exit", Location = new Point(160, 4), Width = 100, Height = 30, BackColor = Color.Crimson, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) };
        btnExit.Click += (_, _) => { _closing = true; Close(); };
        btnRow.Controls.Add(btnExit);
        body.Controls.Add(btnRow);

        Controls.Add(body);
    }

    private void ScheduleList_DoubleClick(object? sender, MouseEventArgs e)
    {
        if (scheduleList.SelectedItem is ScheduleDisplay sd)
        {
            using var dlg = new ScheduleEditDialog(sd.Point);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshSchedule();
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

    private class ScheduleEditDialog : Form
    {
        public ScheduleEditDialog(SchedulePoint pt)
        {
            Text = "Edit Schedule Point";
            Size = new Size(300, 180);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;

            new Label { Text = "Time (HH:MM):", Bounds = new Rectangle(14, 18, 100, 22), Parent = this };
            var txtTime = new TextBox { Text = pt.Time, Bounds = new Rectangle(120, 16, 80, 24), Parent = this };

            new Label { Text = "Brightness (0-100):", Bounds = new Rectangle(14, 50, 120, 22), Parent = this };
            var txtBrightness = new TextBox { Text = pt.Brightness.ToString(), Bounds = new Rectangle(120, 48, 60, 24), Parent = this };

            var ok = new Button { Text = "OK", Bounds = new Rectangle(70, 90, 75, 28), Parent = this };
            ok.Click += (_, _) =>
            {
                pt.Time = txtTime.Text;
                if (int.TryParse(txtBrightness.Text, out var b))
                    pt.Brightness = Math.Clamp(b, 0, 100);
                DialogResult = DialogResult.OK;
                Close();
            };

            var cancel = new Button { Text = "Cancel", Bounds = new Rectangle(155, 90, 75, 28), Parent = this };
            cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
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
