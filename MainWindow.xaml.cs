using System.Windows;
using System.Windows.Controls;
using BrightTime.Models;
using BrightTime.Services;

namespace BrightTime;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly LogService _log;
    private readonly BrightnessController _brightness;
    private readonly ScheduleService _scheduleService;
    private readonly SettingsService _settingsService;
    private readonly StartupService _startupService;
    private readonly List<SchedulePoint> _editingSchedule = new();

    public event Action? ExitRequested;

    public MainWindow(
        AppSettings settings,
        LogService log,
        BrightnessController brightness,
        ScheduleService scheduleService,
        SettingsService settingsService,
        StartupService startupService)
    {
        InitializeComponent();

        _settings = settings;
        _log = log;
        _brightness = brightness;
        _scheduleService = scheduleService;
        _settingsService = settingsService;
        _startupService = startupService;

        LoadSettings();
        UpdateStatus();
        PopulateScheduleList();

        _log.Info("MainWindow initialized");
    }

    public void UpdateStatus()
    {
        StatusCurrentBrightness.Text = $"Current brightness: {_brightness.CurrentBrightness}%";
        StatusTargetBrightness.Text = $"Target brightness: {_brightness.TargetBrightness}%";
        StatusActiveMethod.Text = $"Active method: {_brightness.ActiveMethod}";
        StatusAutoMode.Text = $"Automatic mode: {(_settings.AutomaticEnabled ? "Enabled" : "Disabled")}";
        StatusMessage.Text = _brightness.StatusMessage;
    }

    private void LoadSettings()
    {
        ChkAutoBrightness.IsChecked = _settings.AutomaticEnabled;
        SliderBrightness.Value = _settings.LastManualBrightness;
        TxtBrightnessValue.Text = _settings.LastManualBrightness.ToString();
        ChkSmoothTransition.IsChecked = _settings.SmoothTransitionEnabled;
        ChkRestoreBrightness.IsChecked = _settings.RestoreBrightnessOnExit;
        ChkStartWithWindows.IsChecked = _startupService.IsEnabled();
        ChkUseOverlay.IsChecked = _settings.UseOverlayFallback;
    }

    private void PopulateScheduleList()
    {
        _editingSchedule.Clear();
        foreach (var sp in _settings.Schedule)
        {
            _editingSchedule.Add(new SchedulePoint { Time = sp.Time, Brightness = sp.Brightness });
        }
        ScheduleList.ItemsSource = null;
        ScheduleList.ItemsSource = _editingSchedule;
    }

    private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var value = (int)Math.Round(e.NewValue);
        TxtBrightnessValue.Text = value.ToString();
    }

    private async void BtnApplyBrightness_Click(object sender, RoutedEventArgs e)
    {
        var brightness = (int)Math.Round(SliderBrightness.Value);
        _log.Info($"Manual brightness set to {brightness}");

        _settings.LastManualBrightness = brightness;
        _settings.ManualOverrideUntil = DateTime.Now.AddMinutes(_settings.ManualOverrideMinutes);

        if (ChkSmoothTransition.IsChecked == true)
        {
            await _brightness.SmoothTransitionAsync(_brightness.CurrentBrightness, brightness, 10, 200);
        }
        else
        {
            await _brightness.SetBrightnessAsync(brightness);
        }

        _settingsService.Save(_settings);
        UpdateStatus();
    }

    private void ChkAutoBrightness_CheckedChanged(object sender, RoutedEventArgs e)
    {
        _settings.AutomaticEnabled = ChkAutoBrightness.IsChecked == true;
        if (_settings.AutomaticEnabled)
        {
            _settings.ManualOverrideUntil = null;
        }
        _settingsService.Save(_settings);
        UpdateStatus();
    }

    private void ChkStartWithWindows_Checked(object sender, RoutedEventArgs e)
    {
        _startupService.Enable();
        _settings.StartWithWindows = true;
        _settingsService.Save(_settings);
        _log.Info("Start with Windows enabled");
    }

    private void ChkStartWithWindows_Unchecked(object sender, RoutedEventArgs e)
    {
        _startupService.Disable();
        _settings.StartWithWindows = false;
        _settingsService.Save(_settings);
        _log.Info("Start with Windows disabled");
    }

    private void BtnAddSchedulePoint_Click(object sender, RoutedEventArgs e)
    {
        _editingSchedule.Add(new SchedulePoint { Time = "12:00", Brightness = 50 });
        ScheduleList.ItemsSource = null;
        ScheduleList.ItemsSource = _editingSchedule;
    }

    private void BtnRemoveSchedulePoint_Click(object sender, RoutedEventArgs e)
    {
        if (ScheduleList.SelectedItem is SchedulePoint sp)
        {
            _editingSchedule.Remove(sp);
            ScheduleList.ItemsSource = null;
            ScheduleList.ItemsSource = _editingSchedule;
        }
    }

    private void BtnSaveSchedule_Click(object sender, RoutedEventArgs e)
    {
        _settings.Schedule.Clear();
        foreach (var sp in _editingSchedule)
        {
            _settings.Schedule.Add(new SchedulePoint { Time = sp.Time, Brightness = sp.Brightness });
        }
        _settingsService.Save(_settings);
        _log.Info("Schedule saved");
        System.Windows.MessageBox.Show("Schedule saved successfully.", "BrightTime", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnMinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        _log.Info("Window minimized to tray");
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
        base.OnStateChanged(e);
    }
}
