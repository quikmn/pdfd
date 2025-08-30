using System;

namespace PDFd.Domain.Models;

/// <summary>
/// Represents a single file processing history entry
/// </summary>
public sealed record ProcessingHistoryItem
{
    public required string FileName { get; init; }
    public required string Operation { get; init; }
    public required bool Success { get; init; }
    public required DateTime ProcessedAt { get; init; }
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }
    public long? FileSizeBytes { get; init; }
    
    public string Status => Success ? "✓" : "✗";
    public string StatusColor => Success ? "#10b981" : "#ef4444";
}
