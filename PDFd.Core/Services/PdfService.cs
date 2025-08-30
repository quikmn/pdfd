namespace PDFd.Core.Services;

/// <summary>
/// Main service layer for PDF operations. Simple interface for the UI.
/// </summary>
public sealed class PdfService : IDisposable
{
    private readonly IPdfProcessor _processor;
    private readonly IBatchProcessor _batchProcessor;
    private readonly List<Domain.Models.ProcessingHistoryItem> _history = new();
    private bool _disposed;
    
    public string? OutputDirectory { get; set; }
    public IReadOnlyList<Domain.Models.ProcessingHistoryItem> ProcessingHistory => _history;
    
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
            var failureResult = ProcessingResult.CreateFailure(
                "CONVERT",
                $"Could not read PDF: {Path.GetFileName(filePath)}");
            
            AddToHistory(filePath, "Convert to Word", false, null, failureResult.ErrorMessage);
            return failureResult;
        }
        
        // Use custom output directory if set
        var optionsWithOutput = (options ?? ConversionOptions.Default) with 
        { 
            OutputDirectory = OutputDirectory 
        };
        
        var result = await _processor.ConvertToWordAsync(metadata, optionsWithOutput, ct);
        
        AddToHistory(filePath, "Convert to Word", result.Success, 
            result.OutputPath, result.ErrorMessage, metadata.SizeInBytes);
        
        return result;
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
            var failureResult = ProcessingResult.CreateFailure(
                "COMPRESS",
                $"Could not read PDF: {Path.GetFileName(filePath)}");
            
            AddToHistory(filePath, "Compress", false, null, failureResult.ErrorMessage);
            return failureResult;
        }
        
        var result = await _processor.CompressAsync(
            metadata,
            options ?? CompressionOptions.Balanced,
            ct);
        
        AddToHistory(filePath, "Compress", result.Success, 
            result.OutputPath, result.ErrorMessage, metadata.SizeInBytes);
        
        return result;
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
        Func<Domain.Models.PdfDocument, CancellationToken, Task<ProcessingResult>> operation = operationType switch
        {
            BatchOperationType.ConvertToWord => async (doc, token) => 
            {
                var options = ConversionOptions.Default with { OutputDirectory = OutputDirectory };
                var result = await _processor.ConvertToWordAsync(doc, options, token);
                AddToHistory(doc.FilePath, "Convert to Word", result.Success, 
                    result.OutputPath, result.ErrorMessage, doc.SizeInBytes);
                return result;
            },
            BatchOperationType.Compress => async (doc, token) => 
            {
                var result = await _processor.CompressAsync(doc, CompressionOptions.Balanced, token);
                AddToHistory(doc.FilePath, "Compress", result.Success, 
                    result.OutputPath, result.ErrorMessage, doc.SizeInBytes);
                return result;
            },
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
    public async Task<Domain.Models.PdfDocument?> GetPdfMetadataAsync(string filePath, CancellationToken ct = default)
    {
        return await _processor.GetMetadataAsync(filePath, ct);
    }
    
    /// <summary>
    /// Clear processing history
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }
    
    /// <summary>
    /// Get recent history items
    /// </summary>
    public IEnumerable<Domain.Models.ProcessingHistoryItem> GetRecentHistory(int count = 10)
    {
        return _history.OrderByDescending(h => h.ProcessedAt).Take(count);
    }
    
    private void AddToHistory(string filePath, string operation, bool success, 
        string? outputPath, string? errorMessage, long? fileSizeBytes = null)
    {
        _history.Add(new Domain.Models.ProcessingHistoryItem
        {
            FileName = Path.GetFileName(filePath),
            Operation = operation,
            Success = success,
            ProcessedAt = DateTime.Now,
            OutputPath = outputPath,
            ErrorMessage = errorMessage,
            FileSizeBytes = fileSizeBytes
        });
        
        // Keep only last 100 items
        if (_history.Count > 100)
        {
            _history.RemoveAt(0);
        }
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
