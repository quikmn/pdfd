using PDFd.Domain.Models;

namespace PDFd.Domain.Interfaces;

public interface IBatchProcessor
{
    Task<BatchOperation> ProcessBatchAsync(
        IEnumerable<string> filePaths,
        Func<PdfDocument, CancellationToken, Task<ProcessingResult>> operation,
        IProgress<BatchOperation>? progress = null,
        CancellationToken ct = default);
    
    Task<TimeSpan> EstimateProcessingTimeAsync(
        IEnumerable<string> filePaths,
        string operationType,
        CancellationToken ct = default);
}
