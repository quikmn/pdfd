namespace PDFd.Domain.Models;

/// <summary>
/// Result of any PDF operation. We don't hide failures.
/// </summary>
public sealed record ProcessingResult
{
    public required bool Success { get; init; }
    public required string OperationType { get; init; }
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public required TimeSpan ProcessingTime { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
    
    public static ProcessingResult CreateSuccess(
        string operationType, 
        string outputPath, 
        TimeSpan processingTime,
        Dictionary<string, object>? metrics = null) => new()
    {
        Success = true,
        OperationType = operationType,
        OutputPath = outputPath,
        ProcessingTime = processingTime,
        Metrics = metrics ?? new()
    };
    
    public static ProcessingResult CreateFailure(
        string operationType,
        string errorMessage,
        Exception? exception = null) => new()
    {
        Success = false,
        OperationType = operationType,
        ErrorMessage = errorMessage,
        Exception = exception,
        ProcessingTime = TimeSpan.Zero
    };
}
