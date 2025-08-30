namespace PDFd.Domain.Models;

public sealed record BatchOperation
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required List<string> FilePaths { get; init; }
    public required string OperationType { get; init; }
    public required DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int TotalFiles => FilePaths.Count;
    public int ProcessedFiles { get; init; }
    public int SuccessfulFiles { get; init; }
    public int FailedFiles { get; init; }
    public bool IsCancelled { get; init; }
    
    public double ProgressPercentage => 
        TotalFiles > 0 ? (ProcessedFiles / (double)TotalFiles) * 100 : 0;
    
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    
    public BatchStatus Status => (ProcessedFiles, IsCancelled, CompletedAt) switch
    {
        (_, true, _) => BatchStatus.Cancelled,
        (_, _, not null) => BatchStatus.Completed,
        (0, _, _) => BatchStatus.Pending,
        _ => BatchStatus.Processing
    };
}

public enum BatchStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled,
    Failed
}
