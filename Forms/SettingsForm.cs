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

        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);

        Text = "BrightTime";
        Size = new Size(900, 900);
        MinimumSize = new Size(800, 800);
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += SettingsForm_FormClosing;

        BuildUI();
        LoadSettings();

        log.Info("SettingsForm opened");
    }

    private void BuildUI()
    {
        var y = 16;
        var w = 840;
        var pad = 16;
        var rowH = 28;
        var gap = 8;

        var title = new Label
        {
            Text = "BrightTime",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            Bounds = new Rectangle(pad, y, w, 40)
        };
        Controls.Add(title);
        y += 48;

        lblCurrent = MakeLabel($"Current brightness: --", pad, y, w, rowH); y += rowH + gap;
        lblTarget = MakeLabel($"Target brightness: --", pad, y, w, rowH); y += rowH + gap;
        lblMethod = MakeLabel($"Active method: --", pad, y, w, rowH); y += rowH + gap;
        lblAutoMode = MakeLabel($"Automatic mode: --", pad, y, w, rowH); y += rowH + 16;

        var grp = new GroupBox { Text = " Controls ", Bounds = new Rectangle(pad, y, w, 320) };
        grp.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        var gy = 28;

        chkAuto = new CheckBox { Text = "Enable automatic brightness", Bounds = new Rectangle(12, gy, 400, 28), Font = new Font("Segoe UI", 11f) };
        chkAuto.CheckedChanged += (_, _) => { _settings.AutomaticEnabled = chkAuto.Checked; UpdateAutoLabel(); };
        grp.Controls.Add(chkAuto);
        gy += 36;

        var lblTrk = new Label { Text = "Manual brightness:", Bounds = new Rectangle(12, gy, 200, 24), Font = new Font("Segoe UI", 11f) };
        grp.Controls.Add(lblTrk);
        gy += 28;

        lblBrightnessVal = new Label { Text = "100", Bounds = new Rectangle(w - 70, gy, 50, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 12f, FontStyle.Bold) };
        trackBrightness = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, Bounds = new Rectangle(12, gy, w - 90, 45), TickFrequency = 10, LargeChange = 10 };
        trackBrightness.ValueChanged += (_, _) => lblBrightnessVal.Text = trackBrightness.Value.ToString();
        grp.Controls.Add(lblBrightnessVal);
        grp.Controls.Add(trackBrightness);
        gy += 52;

        btnApply = new Button { Text = "Apply Manual Brightness", Bounds = new Rectangle(12, gy, 240, 34), Font = new Font("Segoe UI", 11f) };
        btnApply.Click += BtnApply_Click;
        grp.Controls.Add(btnApply);
        gy += 42;

        chkSmooth = new CheckBox { Text = "Smooth transition", Bounds = new Rectangle(12, gy, 280, 28), Font = new Font("Segoe UI", 11f) };
        grp.Controls.Add(chkSmooth);
        gy += 34;

        chkOverlay = new CheckBox { Text = "Use overlay fallback", Bounds = new Rectangle(12, gy, 280, 28), Font = new Font("Segoe UI", 11f) };
        grp.Controls.Add(chkOverlay);
        gy += 34;

        chkRestore = new CheckBox { Text = "Restore brightness on exit", Bounds = new Rectangle(12, gy, 320, 28), Font = new Font("Segoe UI", 11f) };
        grp.Controls.Add(chkRestore);
        gy += 34;

        chkStartup = new CheckBox { Text = "Start with Windows", Bounds = new Rectangle(12, gy, 280, 28), Font = new Font("Segoe UI", 11f) };
        chkStartup.CheckedChanged += ChkStartup_Changed;
        grp.Controls.Add(chkStartup);

        Controls.Add(grp);
        y += grp.Height + 16;

        var grp2 = new GroupBox { Text = " Schedule ", Bounds = new Rectangle(pad, y, w, 280) };
        grp2.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        scheduleList = new ListBox { Bounds = new Rectangle(12, 28, w - 30, 140), Font = new Font("Segoe UI", 11f) };
        _scheduleSource.DataSource = new List<SchedulePoint>();
        scheduleList.DataSource = _scheduleSource;
        scheduleList.MouseDoubleClick += ScheduleList_DoubleClick;
        grp2.Controls.Add(scheduleList);

        btnAdd = new Button { Text = "Add Point", Bounds = new Rectangle(12, 180, 110, 32), Font = new Font("Segoe UI", 11f) };
        btnAdd.Click += BtnAdd_Click;
        grp2.Controls.Add(btnAdd);

        btnRemove = new Button { Text = "Remove Selected", Bounds = new Rectangle(130, 180, 140, 32), Font = new Font("Segoe UI", 11f) };
        btnRemove.Click += BtnRemove_Click;
        grp2.Controls.Add(btnRemove);

        btnSave = new Button { Text = "Save Schedule", Bounds = new Rectangle(w - 130, 235, 120, 32), Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
        btnSave.Click += BtnSave_Click;
        grp2.Controls.Add(btnSave);

        Controls.Add(grp2);
        y += grp2.Height + 16;

        lblStatus = new Label { Text = "", Bounds = new Rectangle(pad, y, w, 28), ForeColor = Color.DarkOrange, Font = new Font("Segoe UI", 11f) };
        Controls.Add(lblStatus);
        y += 36;

        btnMinimize = new Button { Text = "Minimize to Tray", Bounds = new Rectangle(pad, y, 180, 36), Font = new Font("Segoe UI", 11f) };
        btnMinimize.Click += (_, _) => { _closing = false; Close(); };
        Controls.Add(btnMinimize);

        btnExit = new Button { Text = "Exit", Bounds = new Rectangle(pad + 190, y, 120, 36), BackColor = Color.Crimson, ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
        btnExit.Click += (_, _) => { _closing = true; Close(); };
        Controls.Add(btnExit);
    }

    private Label MakeLabel(string text, int x, int y, int w, int h)
    {
        var lbl = new Label { Text = text, Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", 11f) };
        Controls.Add(lbl);
        return lbl;
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
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 11f);
            Text = "Edit Schedule Point";
            Size = new Size(340, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;

            new Label { Text = "Time (HH:MM):", Bounds = new Rectangle(16, 20, 120, 24), Font = Font, Parent = this };
            var txtTime = new TextBox { Text = pt.Time, Bounds = new Rectangle(140, 18, 100, 26), Font = Font, Parent = this };

            new Label { Text = "Brightness (0-100):", Bounds = new Rectangle(16, 56, 130, 24), Font = Font, Parent = this };
            var txtBrightness = new TextBox { Text = pt.Brightness.ToString(), Bounds = new Rectangle(140, 54, 70, 26), Font = Font, Parent = this };

            var ok = new Button { Text = "OK", Bounds = new Rectangle(100, 100, 80, 30), Font = Font, Parent = this };
            ok.Click += (_, _) =>
            {
                pt.Time = txtTime.Text;
                if (int.TryParse(txtBrightness.Text, out var b))
                    pt.Brightness = Math.Clamp(b, 0, 100);
                DialogResult = DialogResult.OK;
                Close();
            };

            var cancel = new Button { Text = "Cancel", Bounds = new Rectangle(190, 100, 80, 30), Font = Font, Parent = this };
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
