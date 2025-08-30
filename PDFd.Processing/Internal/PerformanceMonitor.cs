namespace PDFd.Processing.Internal;

/// <summary>
/// Monitors performance to ensure we meet our targets.
/// </summary>
internal sealed class PerformanceMonitor
{
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private long _bytesProcessed;
    private int _filesProcessed;
    
    public void Start() => _stopwatch.Start();
    public void Stop() => _stopwatch.Stop();
    
    public void RecordFile(long sizeInBytes)
    {
        _bytesProcessed += sizeInBytes;
        _filesProcessed++;
    }
    
    public double GetMBPerSecond()
    {
        if (_stopwatch.Elapsed.TotalSeconds == 0) return 0;
        var mbProcessed = _bytesProcessed / (1024.0 * 1024.0);
        return mbProcessed / _stopwatch.Elapsed.TotalSeconds;
    }
    
    public bool MeetsPerformanceTarget()
    {
        return GetMBPerSecond() >= PerformanceTargets.MinProcessingMBPerSecond;
    }
}
