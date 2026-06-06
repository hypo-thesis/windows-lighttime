namespace BrightTimeTray.Services;

public class SingleInstanceService : IDisposable
{
    private readonly Mutex _mutex;
    private bool _owned;

    public SingleInstanceService()
    {
        _mutex = new Mutex(true, "BrightTimeTray.SingleInstance", out _owned);
    }

    public bool IsFirst => _owned;

    public void Dispose()
    {
        if (_owned) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
