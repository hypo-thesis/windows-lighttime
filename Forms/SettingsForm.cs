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
        Size = new Size(720, 800);
        MinimumSize = new Size(600, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += SettingsForm_FormClosing;

        BuildUI();
        LoadSettings();

        log.Info("SettingsForm opened");
    }

    private void BuildUI()
    {
        AutoScroll = true;
        var pad = 12;
        var rowH = 28;
        var gap = 6;
        var cw = ClientSize.Width - pad * 2 - SystemInformation.VerticalScrollBarWidth;
        if (cw < 400) cw = 400;
        var y = 8;

        void AddCtrl(Control c)
        {
            c.Location = new Point(pad, y);
            if (c.Width == 0) c.Width = cw;
            Controls.Add(c);
            y += c.Height + gap;
        }

        var title = new Label { Text = "BrightTime", Font = new Font("Segoe UI", 18f, FontStyle.Bold), Height = 36, Width = cw };
        title.Padding = new Padding(0);
        AddCtrl(title);
        y += 4;

        lblCurrent = new Label { Text = "Current brightness: --", Width = cw, Height = rowH };
        AddCtrl(lblCurrent);
        lblTarget = new Label { Text = "Target brightness: --", Width = cw, Height = rowH };
        AddCtrl(lblTarget);
        lblMethod = new Label { Text = "Active method: --", Width = cw, Height = rowH };
        AddCtrl(lblMethod);
        lblAutoMode = new Label { Text = "Automatic mode: --", Width = cw, Height = rowH };
        AddCtrl(lblAutoMode);
        y += 4;

        var grp = new GroupBox { Text = "Controls", Width = cw, Height = 300 };
        var gy = 24;
        grp.Controls.Add(chkAuto = new CheckBox { Text = "Enable automatic brightness", Location = new Point(12, gy), Width = 350, Height = rowH });
        chkAuto.CheckedChanged += (_, _) => { _settings.AutomaticEnabled = chkAuto.Checked; UpdateAutoLabel(); };
        gy += 34;

        grp.Controls.Add(new Label { Text = "Manual brightness:", Location = new Point(12, gy), Width = 200, Height = 22 });
        gy += 26;

        lblBrightnessVal = new Label { Text = "100", Location = new Point(cw - 70, gy - 2), Width = 50, Height = rowH, TextAlign = ContentAlignment.MiddleRight };
        grp.Controls.Add(lblBrightnessVal);
        trackBrightness = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, Location = new Point(12, gy), Width = cw - 90, Height = 40, TickFrequency = 10 };
        trackBrightness.ValueChanged += (_, _) => lblBrightnessVal.Text = trackBrightness.Value.ToString();
        grp.Controls.Add(trackBrightness);
        gy += 48;

        grp.Controls.Add(btnApply = new Button { Text = "Apply Manual Brightness", Location = new Point(12, gy), Width = 200, Height = 32 });
        btnApply.Click += BtnApply_Click;
        gy += 40;

        grp.Controls.Add(chkSmooth = new CheckBox { Text = "Smooth transition", Location = new Point(12, gy), Width = 250, Height = rowH });
        gy += 32;
        grp.Controls.Add(chkOverlay = new CheckBox { Text = "Use overlay fallback", Location = new Point(12, gy), Width = 250, Height = rowH });
        gy += 32;
        grp.Controls.Add(chkRestore = new CheckBox { Text = "Restore brightness on exit", Location = new Point(12, gy), Width = 280, Height = rowH });
        gy += 32;
        grp.Controls.Add(chkStartup = new CheckBox { Text = "Start with Windows", Location = new Point(12, gy), Width = 250, Height = rowH });
        chkStartup.CheckedChanged += ChkStartup_Changed;

        AddCtrl(grp);
        y += 4;

        var grp2 = new GroupBox { Text = "Schedule", Width = cw, Height = 260 };
        scheduleList = new ListBox { Location = new Point(12, 24), Width = cw - 30, Height = 140 };
        _scheduleSource.DataSource = new List<SchedulePoint>();
        scheduleList.DataSource = _scheduleSource;
        scheduleList.MouseDoubleClick += ScheduleList_DoubleClick;
        grp2.Controls.Add(scheduleList);

        grp2.Controls.Add(btnAdd = new Button { Text = "Add Point", Location = new Point(12, 172), Width = 100, Height = 30 });
        btnAdd.Click += BtnAdd_Click;
        grp2.Controls.Add(btnRemove = new Button { Text = "Remove Selected", Location = new Point(120, 172), Width = 130, Height = 30 });
        btnRemove.Click += BtnRemove_Click;
        grp2.Controls.Add(btnSave = new Button { Text = "Save Schedule", Location = new Point(cw - 120, 220), Width = 110, Height = 30, Font = new Font(Font, FontStyle.Bold) });
        btnSave.Click += BtnSave_Click;

        AddCtrl(grp2);
        y += 4;

        lblStatus = new Label { Text = "", Width = cw, Height = rowH, ForeColor = Color.DarkOrange };
        AddCtrl(lblStatus);
        y += 4;

        var btnPanel = new Panel { Width = cw, Height = 40 };
        btnMinimize = new Button { Text = "Minimize to Tray", Location = new Point(0, 4), Width = 150, Height = 32 };
        btnMinimize.Click += (_, _) => { _closing = false; Close(); };
        btnPanel.Controls.Add(btnMinimize);

        btnExit = new Button { Text = "Exit", Location = new Point(160, 4), Width = 100, Height = 32, BackColor = Color.Crimson, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) };
        btnExit.Click += (_, _) => { _closing = true; Close(); };
        btnPanel.Controls.Add(btnExit);

        btnPanel.Height = 40;
        AddCtrl(btnPanel);

        AutoScrollMinSize = new Size(0, y + 8);
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
