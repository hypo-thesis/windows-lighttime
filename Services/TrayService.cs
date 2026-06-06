using System.Windows;
using BrightTime.Models;

namespace BrightTime.Services;

public class TrayService : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.ContextMenuStrip _contextMenu;
    private readonly AppSettings _settings;
    private readonly LogService _log;
    private readonly BrightnessController _brightness;

    public event Action? ShowWindowRequested;
    public event Action? ExitRequested;
    public event Action<bool>? ToggleAutoRequested;
    public event Action<int>? SetBrightnessRequested;
    public event Action? RestorePreviousBrightnessRequested;

    public TrayService(AppSettings settings, LogService log, BrightnessController brightness)
    {
        _settings = settings;
        _log = log;
        _brightness = brightness;

        _contextMenu = new System.Windows.Forms.ContextMenuStrip();

        var showItem = new System.Windows.Forms.ToolStripMenuItem("Show");
        showItem.Click += (_, _) => ShowWindowRequested?.Invoke();
        _contextMenu.Items.Add(showItem);

        var autoItem = new System.Windows.Forms.ToolStripMenuItem(
            settings.AutomaticEnabled ? "Disable Automatic Brightness" : "Enable Automatic Brightness");
        autoItem.Click += (_, _) =>
        {
            var enable = !_settings.AutomaticEnabled;
            _settings.AutomaticEnabled = enable;
            autoItem.Text = enable ? "Disable Automatic Brightness" : "Enable Automatic Brightness";
            ToggleAutoRequested?.Invoke(enable);
        };
        _contextMenu.Items.Add(autoItem);
        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        AddBrightnessItem("Set Brightness 25%", 25);
        AddBrightnessItem("Set Brightness 50%", 50);
        AddBrightnessItem("Set Brightness 75%", 75);
        AddBrightnessItem("Set Brightness 100%", 100);
        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var restoreItem = new System.Windows.Forms.ToolStripMenuItem("Restore Previous Brightness");
        restoreItem.Click += (_, _) => RestorePreviousBrightnessRequested?.Invoke();
        _contextMenu.Items.Add(restoreItem);
        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "BrightTime",
            Icon = LoadIcon(),
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindowRequested?.Invoke();
    }

    private void AddBrightnessItem(string text, int brightness)
    {
        var item = new System.Windows.Forms.ToolStripMenuItem(text);
        item.Click += (_, _) => SetBrightnessRequested?.Invoke(brightness);
        _contextMenu.Items.Add(item);
    }

    private System.Drawing.Icon LoadIcon()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("BrightTime.Assets.app.ico");
            if (stream != null)
                return new System.Drawing.Icon(stream);
        }
        catch
        {
        }

        using var bmp = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.Transparent);
        g.FillEllipse(System.Drawing.Brushes.Gold, 1, 1, 14, 14);
        g.DrawEllipse(System.Drawing.Pens.DarkOrange, 1, 1, 14, 14);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
