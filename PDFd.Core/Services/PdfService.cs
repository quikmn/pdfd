namespace PDFd.Core.Services;

/// <summary>
/// Main service layer for PDF operations. Simple interface for the UI.
/// </summary>
public sealed class PdfService : IDisposable
{
    private readonly IPdfProcessor _processor;
    private readonly IBatchProcessor _batchProcessor;
    private bool _disposed;
    
    public PdfService()
    {
        _processor = new PdfProcessor();
        _batchProcessor = new BatchProcessor(_processor);
    }
    
    /// <summary>
    /// Convert a single PDF to Word
    /// </summary>
    public async Task<ProcessingResult> ConvertToWordAsync(
        string filePath,
        ConversionOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        
        var metadata = await _processor.GetMetadataAsync(filePath, ct);
        if (metadata == null)
        {
            return ProcessingResult.CreateFailure(
                "CONVERT",
                $"Could not read PDF: {Path.GetFileName(filePath)}");
        }
        
        return await _processor.ConvertToWordAsync(
            metadata,
            options ?? ConversionOptions.Default,
            ct);
    }
    
    /// <summary>
    /// Compress a single PDF
    /// </summary>
    public async Task<ProcessingResult> CompressPdfAsync(
        string filePath,
        CompressionOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        
        var metadata = await _processor.GetMetadataAsync(filePath, ct);
        if (metadata == null)
        {
            return ProcessingResult.CreateFailure(
                "COMPRESS",
                $"Could not read PDF: {Path.GetFileName(filePath)}");
        }
        
        return await _processor.CompressAsync(
            metadata,
            options ?? CompressionOptions.Balanced,
            ct);
    }
    
    /// <summary>
    /// Process multiple files
    /// </summary>
    public async Task<BatchOperation> ProcessBatchAsync(
        IEnumerable<string> filePaths,
        BatchOperationType operationType,
        IProgress<BatchOperation>? progress = null,
        CancellationToken ct = default)
    {
        Func<PdfDocument, CancellationToken, Task<ProcessingResult>> operation = operationType switch
        {
            BatchOperationType.ConvertToWord => (doc, token) => 
                _processor.ConvertToWordAsync(doc, ConversionOptions.Default, token),
            BatchOperationType.Compress => (doc, token) => 
                _processor.CompressAsync(doc, CompressionOptions.Balanced, token),
            _ => throw new ArgumentException($"Unknown operation: {operationType}")
        };
        
        return await _batchProcessor.ProcessBatchAsync(filePaths, operation, progress, ct);
    }
    
    /// <summary>
    /// Validate if a file is a processable PDF
    /// </summary>
    public async Task<bool> ValidatePdfAsync(string filePath, CancellationToken ct = default)
    {
        return await _processor.ValidateAsync(filePath, ct);
    }
    
    /// <summary>
    /// Get PDF metadata without processing
    /// </summary>
    public async Task<PdfDocument?> GetPdfMetadataAsync(string filePath, CancellationToken ct = default)
    {
        return await _processor.GetMetadataAsync(filePath, ct);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            (_processor as IDisposable)?.Dispose();
            _disposed = true;
        }
    }
}

public enum BatchOperationType
{
    ConvertToWord,
    Compress
}
