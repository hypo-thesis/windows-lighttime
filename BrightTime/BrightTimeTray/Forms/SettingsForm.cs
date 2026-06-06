using System.Diagnostics;
using BrightTime.Models;
using BrightTime.Services;

namespace BrightTimeTray.Forms;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly BrightnessController _brightness;
    private readonly ScheduleService _scheduleService;
    private readonly BrightTimeTray.Services.StartupService _startupService;
    private readonly LogService _log;

    private CheckBox chkAuto = null!;
    private TrackBar trackBrightness = null!;
    private Label lblBrightnessVal = null!;
    private Button btnApply = null!;
    private CheckBox chkOverlay = null!;
    private CheckBox chkRestore = null!;
    private CheckBox chkStartup = null!;
    private ListBox scheduleList = null!;
    private Button btnAdd = null!;
    private Button btnRemove = null!;
    private Button btnSave = null!;
    private Button btnMinimize = null!;
    private Button btnExit = null!;
    private Button btnInstallTask = null!;
    private Button btnRemoveTask = null!;
    private Button btnApplyNow = null!;
    private Label lblStatus = null!;
    private Label lblTask = null!;
    private Label lblOverlayWarn = null!;
    private Label lblCurrent = null!;
    private Label lblTarget = null!;
    private Label lblMethod = null!;
    private Label lblAutoMode = null!;

    private readonly BindingSource _scheduleSource = new();

    public event Action? ExitRequested;
    public event Action? ApplyNowRequested;
    public event Action? InstallTaskRequested;
    public event Action? RemoveTaskRequested;

    public SettingsForm(AppSettings settings, SettingsService ss, BrightnessController bc,
        ScheduleService sc, BrightTimeTray.Services.StartupService su, LogService log)
    {
        _settings = settings;
        _settingsService = ss;
        _brightness = bc;
        _scheduleService = sc;
        _startupService = su;
        _log = log;

        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "BrightTime";
        MinimumSize = new Size(520, 620);
        Size = new Size(600, 1200);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScroll = true;
        Font = new Font("Segoe UI", 9F, GraphicsUnit.Point);
        FormClosing += SettingsForm_FormClosing;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        BuildUI();
        LoadSettings();
        UpdateStatus();
    }

    private void BuildUI()
    {
        var fnt = Font;
        var boldFnt = new Font(fnt.FontFamily, 18f, FontStyle.Bold);
        var labelFnt = fnt;

        var tlp = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16),
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label { Text = "BrightTime", Font = boldFnt, AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        tlp.Controls.Add(title, 0, 0);

        var statusPanel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 8) };
        statusPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblCurrent = new Label { Font = labelFnt, Text = "Current brightness: --", AutoSize = true };
        lblTarget = new Label { Font = labelFnt, Text = "Target brightness: --", AutoSize = true };
        lblMethod = new Label { Font = labelFnt, Text = "Active method: --", AutoSize = true };
        lblAutoMode = new Label { Font = labelFnt, Text = "Automatic mode: --", AutoSize = true };
        statusPanel.Controls.Add(lblCurrent, 0, 0);
        statusPanel.Controls.Add(lblTarget, 0, 1);
        statusPanel.Controls.Add(lblMethod, 0, 2);
        statusPanel.Controls.Add(lblAutoMode, 0, 3);
        tlp.Controls.Add(statusPanel, 0, 1);

        var g1 = new GroupBox { Text = "Controls", Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 8, 0, 8) };
        var controlsInner = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6), Margin = Padding.Empty };
        controlsInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controlsInner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int i = 0; i < 8; i++) controlsInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        int row = 0;
        chkAuto = new CheckBox { Font = labelFnt, Text = "Enable automatic brightness", AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
        controlsInner.Controls.Add(chkAuto, 0, row); controlsInner.SetColumnSpan(chkAuto, 2);
        chkAuto.CheckedChanged += (_, _) => { _settings.AutomaticEnabled = chkAuto.Checked; UpdateAutoLabel(); };
        row++;

        controlsInner.Controls.Add(new Label { Font = labelFnt, Text = "Manual brightness:", AutoSize = true, Margin = new Padding(0, 4, 4, 2) }, 0, row);
        lblBrightnessVal = new Label { Font = labelFnt, Text = "100", AutoSize = true, Margin = new Padding(0, 4, 0, 2), TextAlign = ContentAlignment.MiddleRight };
        controlsInner.Controls.Add(lblBrightnessVal, 1, row);
        row++;

        trackBrightness = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 2) };
        controlsInner.Controls.Add(trackBrightness, 0, row); controlsInner.SetColumnSpan(trackBrightness, 2);
        trackBrightness.ValueChanged += (_, _) => lblBrightnessVal.Text = trackBrightness.Value.ToString();
        row++;

        btnApply = new Button { Font = labelFnt, Text = "Apply Manual Brightness", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        controlsInner.Controls.Add(btnApply, 0, row); controlsInner.SetColumnSpan(btnApply, 2);
        btnApply.Click += BtnApply_Click;
        row++;

        chkOverlay = new CheckBox { Font = labelFnt, Text = "Use overlay fallback (tray mode only)", AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
        controlsInner.Controls.Add(chkOverlay, 0, row); controlsInner.SetColumnSpan(chkOverlay, 2);
        row++;

        chkRestore = new CheckBox { Font = labelFnt, Text = "Restore brightness on exit", AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
        controlsInner.Controls.Add(chkRestore, 0, row); controlsInner.SetColumnSpan(chkRestore, 2);
        row++;

        chkStartup = new CheckBox { Font = labelFnt, Text = "Start with Windows", AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
        controlsInner.Controls.Add(chkStartup, 0, row); controlsInner.SetColumnSpan(chkStartup, 2);
        chkStartup.CheckedChanged += ChkStartup_Changed;
        g1.Controls.Add(controlsInner);
        tlp.Controls.Add(g1, 0, 2);

        var g2 = new GroupBox { Text = "Schedule", Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 8, 0, 8) };
        var scheduleInner = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6), Margin = Padding.Empty };
        scheduleInner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        scheduleInner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        scheduleInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scheduleInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scheduleInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        scheduleList = new ListBox { Font = labelFnt, Height = 150, Margin = new Padding(0, 0, 0, 4), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        _scheduleSource.DataSource = new List<SchedulePoint>();
        scheduleList.DataSource = _scheduleSource;
        scheduleList.MouseDoubleClick += ScheduleList_DoubleClick;
        scheduleInner.Controls.Add(scheduleList, 0, 0); scheduleInner.SetColumnSpan(scheduleList, 2);

        btnAdd = new Button { Font = labelFnt, Text = "Add Point", AutoSize = true, Margin = new Padding(0, 0, 8, 4) };
        btnAdd.Click += BtnAdd_Click;
        scheduleInner.Controls.Add(btnAdd, 0, 1);

        btnRemove = new Button { Font = labelFnt, Text = "Remove Selected", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        btnRemove.Click += BtnRemove_Click;
        scheduleInner.Controls.Add(btnRemove, 1, 1);

        var btnScheduleSave = new Button { Font = labelFnt, Text = "Save Schedule", AutoSize = true };
        btnScheduleSave.Click += (_, _) => { _settingsService.Save(_settings); MessageBox.Show("Schedule saved.", "BrightTime", MessageBoxButtons.OK, MessageBoxIcon.Information); };
        scheduleInner.Controls.Add(btnScheduleSave, 1, 2); scheduleInner.SetColumnSpan(btnScheduleSave, 2);
        btnScheduleSave.Anchor = AnchorStyles.Right;
        g2.Controls.Add(scheduleInner);
        tlp.Controls.Add(g2, 0, 3);

        var taskGroup = new GroupBox { Text = "Scheduled Task (0 MB idle mode)", Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 8, 0, 8) };
        var taskInner = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6) };
        taskInner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        taskInner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        taskInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        taskInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        taskInner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblTask = new Label { Font = labelFnt, Text = "Status: checking...", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        taskInner.Controls.Add(lblTask, 0, 0); taskInner.SetColumnSpan(lblTask, 2);

        btnInstallTask = new Button { Font = labelFnt, Text = "Install Task", AutoSize = true, Margin = new Padding(0, 0, 8, 4) };
        btnInstallTask.Click += (_, _) => InstallTaskRequested?.Invoke();
        taskInner.Controls.Add(btnInstallTask, 0, 1);

        btnRemoveTask = new Button { Font = labelFnt, Text = "Remove Task", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        btnRemoveTask.Click += (_, _) => RemoveTaskRequested?.Invoke();
        taskInner.Controls.Add(btnRemoveTask, 1, 1);

        btnApplyNow = new Button { Font = labelFnt, Text = "Run Apply Now", AutoSize = true, Margin = new Padding(0, 0, 0, 0) };
        btnApplyNow.Click += (_, _) => ApplyNowRequested?.Invoke();
        taskInner.Controls.Add(btnApplyNow, 0, 2);

        taskGroup.Controls.Add(taskInner);
        tlp.Controls.Add(taskGroup, 0, 4);

        lblOverlayWarn = new Label
        {
            Font = labelFnt,
            Text = "Overlay fallback requires BrightTimeTray to stay running. Scheduled zero-idle mode supports hardware brightness only.",
            AutoSize = true,
            ForeColor = Color.DarkOrange,
            MaximumSize = new Size(ClientSize.Width - 48, 0),
            Margin = new Padding(0, 4, 0, 4),
        };
        tlp.Controls.Add(lblOverlayWarn, 0, 5);

        lblStatus = new Label { Font = labelFnt, Text = "", AutoSize = true, ForeColor = Color.DarkOrange, Margin = new Padding(0, 4, 0, 0) };
        tlp.Controls.Add(lblStatus, 0, 6);

        Controls.Add(tlp);

        var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8), Margin = Padding.Empty };
        btnSave = new Button { Font = new Font(fnt.FontFamily, 10f), Text = "Save", AutoSize = true, Margin = new Padding(4) };
        btnSave.Click += (_, _) => { _settingsService.Save(_settings); MessageBox.Show("Settings saved.", "BrightTime", MessageBoxButtons.OK, MessageBoxIcon.Information); };
        bottomPanel.Controls.Add(btnSave);

        btnMinimize = new Button { Font = new Font(fnt.FontFamily, 10f), Text = "Minimize to Tray", AutoSize = true, Margin = new Padding(4) };
        btnMinimize.Click += (_, _) => Close();
        bottomPanel.Controls.Add(btnMinimize);

        btnExit = new Button { Font = new Font(fnt.FontFamily, 10f), Text = "Exit", AutoSize = true, Margin = new Padding(4), BackColor = Color.Crimson, ForeColor = Color.White };
        btnExit.Click += (_, _) => { ExitRequested?.Invoke(); Close(); };
        bottomPanel.Controls.Add(btnExit);

        Controls.Add(bottomPanel);
    }

    public void UpdateStatus()
    {
        if (IsDisposed) return;
        lblCurrent.Text = $"Current brightness: {_brightness.CurrentBrightness}%";
        lblTarget.Text = $"Target brightness: {_brightness.TargetBrightness}%";
        lblMethod.Text = $"Active method: {_brightness.ActiveMethod}";
        lblAutoMode.Text = $"Automatic mode: {(_settings.AutomaticEnabled ? "Enabled" : "Disabled")}";
        var status = _brightness.Status;
        if (_brightness.IsOverlayActive)
            status += "  |  Overlay is visual dimming, only works while Tray is running";
        lblStatus.Text = status;
        UpdateTaskStatus();
        lblOverlayWarn.Visible = !_brightness.IsOverlayActive;
    }

    private void UpdateTaskStatus()
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks", "/query /tn \"BrightTime\" /fo LIST /v")
            { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p == null) { lblTask.Text = "Status: could not check"; return; }
            p.WaitForExit(3000);
            lblTask.Text = p.ExitCode == 0 ? "Status: installed (runs every 5 min, 0 MB idle)" : "Status: not installed";
        }
        catch { lblTask.Text = "Status: could not check"; }
    }

    private void ScheduleList_DoubleClick(object? sender, MouseEventArgs e)
    {
        int idx = scheduleList.SelectedIndex;
        if (idx >= 0 && idx < _settings.Schedule.Count)
        {
            var pt = _settings.Schedule[idx];
            using var dlg = new Form();
            dlg.Text = "Edit Schedule Point";
            dlg.Size = new Size(360, 192);
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MinimizeBox = false;
            dlg.MaximizeBox = false;

            var mainTlp = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 2, AutoSize = true };
            mainTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            mainTlp.Controls.Add(new Label { Text = "Time (HH:MM):", AutoSize = true }, 0, 0);
            var tt = new TextBox { Text = pt.Time, Width = 80, Margin = new Padding(4) };
            mainTlp.Controls.Add(tt, 1, 0);

            mainTlp.Controls.Add(new Label { Text = "Brightness (0-100):", AutoSize = true }, 0, 1);
            var tb = new TextBox { Text = pt.Brightness.ToString(), Width = 50, Margin = new Padding(4) };
            mainTlp.Controls.Add(tb, 1, 1);

            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            var okb = new Button { Text = "OK", AutoSize = true, Margin = new Padding(4) };
            okb.Click += (_, _) => { pt.Time = tt.Text; if (int.TryParse(tb.Text, out var v)) pt.Brightness = Math.Clamp(v, 0, 100); dlg.DialogResult = DialogResult.OK; dlg.Close(); };
            var cb = new Button { Text = "Cancel", AutoSize = true, Margin = new Padding(4) };
            cb.Click += (_, _) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
            btnPanel.Controls.Add(okb);
            btnPanel.Controls.Add(cb);
            mainTlp.Controls.Add(btnPanel, 0, 2);
            mainTlp.SetColumnSpan(btnPanel, 2);

            dlg.Controls.Add(mainTlp);
            if (dlg.ShowDialog(this) == DialogResult.OK) RefreshSchedule();
        }
    }

    private void LoadSettings()
    {
        chkAuto.Checked = _settings.AutomaticEnabled;
        trackBrightness.Value = _settings.LastManualBrightness;
        chkOverlay.Checked = _settings.UseOverlayFallback;
        chkRestore.Checked = _settings.RestoreBrightnessOnExit;
        chkStartup.Checked = _startupService.IsEnabled();
        RefreshSchedule();
    }

    private void UpdateAutoLabel() =>
        lblAutoMode.Text = $"Automatic mode: {(_settings.AutomaticEnabled ? "Enabled" : "Disabled")}";

    private void RefreshSchedule()
    {
        _scheduleSource.DataSource = _settings.Schedule.Select((sp, i) => new { Display = $"{sp.Time} -> {sp.Brightness}%" }).ToList();
        _scheduleSource.ResetBindings(false);
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
        int idx = scheduleList.SelectedIndex;
        if (idx >= 0 && idx < _settings.Schedule.Count)
        {
            _settings.Schedule.RemoveAt(idx);
            RefreshSchedule();
        }
    }

    private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _settingsService.Save(_settings);
    }
}
