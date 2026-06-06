using System.Runtime.InteropServices;

namespace BrightTime.Services;

public class SingleInstanceService : IDisposable
{
    private readonly Mutex _mutex;
    private bool _hasHandle;

    public SingleInstanceService()
    {
        _mutex = new Mutex(true, "BrightTime_SingleInstanceMutex", out _hasHandle);
    }

    public bool IsFirstInstance => _hasHandle;

    public void Dispose()
    {
        if (_hasHandle)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }

    public void BringExistingToFront()
    {
        var hwnd = FindWindow(null, "BrightTime");
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            FlashWindow(hwnd, true);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

    private const int SW_RESTORE = 9;
}
