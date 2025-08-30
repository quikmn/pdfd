using System.Collections.Concurrent;

namespace PDFd.Processing;

/// <summary>
/// Handles batch operations. 10,000 files without breaking a sweat.
/// </summary>
public sealed class BatchProcessor : IBatchProcessor
{
    private readonly IPdfProcessor _pdfProcessor;
    private readonly int _maxConcurrency;
    
    public BatchProcessor(IPdfProcessor pdfProcessor, int maxConcurrency = 4)
    {
        _pdfProcessor = pdfProcessor ?? throw new ArgumentNullException(nameof(pdfProcessor));
        _maxConcurrency = Math.Max(1, Math.Min(maxConcurrency, Environment.ProcessorCount));
    }
    
    public async Task<BatchOperation> ProcessBatchAsync(
        IEnumerable<string> filePaths,
        Func<Domain.Models.PdfDocument, CancellationToken, Task<ProcessingResult>> operation,
        IProgress<BatchOperation>? progress = null,
        CancellationToken ct = default)
    {
        var paths = filePaths.ToList();
        var batchOp = new BatchOperation
        {
            FilePaths = paths,
            OperationType = PdfConstants.BatchOperation,
            StartedAt = DateTime.UtcNow
        };
        
        var processedCount = 0;
        var successCount = 0;
        var failedCount = 0;
        
        using var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var tasks = new List<Task>();
        
        foreach (var path in paths)
        {
            await semaphore.WaitAsync(ct);
            
            var task = ProcessFileAsync(path, operation, ct).ContinueWith(async t =>
            {
                semaphore.Release();
                
                var result = await t;
                Interlocked.Increment(ref processedCount);
                
                if (result.Success)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failedCount);
                
                // Report progress
                var currentBatch = batchOp with
                {
                    ProcessedFiles = processedCount,
                    SuccessfulFiles = successCount,
                    FailedFiles = failedCount
                };
                
                progress?.Report(currentBatch);
            }, ct);
            
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
        
        return batchOp with
        {
            ProcessedFiles = processedCount,
            SuccessfulFiles = successCount,
            FailedFiles = failedCount,
            CompletedAt = DateTime.UtcNow
        };
    }
    
    private async Task<ProcessingResult> ProcessFileAsync(
        string filePath,
        Func<Domain.Models.PdfDocument, CancellationToken, Task<ProcessingResult>> operation,
        CancellationToken ct)
    {
        try
        {
            var metadata = await _pdfProcessor.GetMetadataAsync(filePath, ct);
            if (metadata == null)
            {
                return ProcessingResult.CreateFailure(
                    PdfConstants.BatchOperation,
                    $"Could not read metadata for {filePath}");
            }
            
            return await operation(metadata, ct);
        }
        catch (Exception ex)
        {
            return ProcessingResult.CreateFailure(
                PdfConstants.BatchOperation,
                $"Failed to process {filePath}: {ex.Message}",
                ex);
        }
    }
    
    public Task<TimeSpan> EstimateProcessingTimeAsync(
        IEnumerable<string> filePaths,
        string operationType,
        CancellationToken ct = default)
    {
        var paths = filePaths.ToList();
        if (!paths.Any()) return Task.FromResult(TimeSpan.Zero);
        
        // Sample first few files to estimate
        var sampleSize = Math.Min(3, paths.Count);
        var totalBytes = 0L;
        
        for (int i = 0; i < sampleSize; i++)
        {
            if (File.Exists(paths[i]))
            {
                var info = new FileInfo(paths[i]);
                totalBytes += info.Length;
            }
        }
        
        // Estimate based on our performance target: 10MB/second
        var totalEstimatedBytes = (totalBytes / sampleSize) * paths.Count;
        var estimatedSeconds = totalEstimatedBytes / (PerformanceTargets.MinProcessingMBPerSecond * 1024 * 1024);
        
        return Task.FromResult(TimeSpan.FromSeconds(estimatedSeconds));
    }
}
