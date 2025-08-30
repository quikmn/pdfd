using System.IO;

namespace PDFd.Domain.Models;

/// <summary>
/// Represents a PDF document. Immutable by default.
/// </summary>
public sealed record PdfDocument
{
    public required string FilePath { get; init; }
    public required long SizeInBytes { get; init; }
    public required int PageCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public byte[]? RawData { get; init; }
    
    public string FileName => Path.GetFileName(FilePath);
    public string FileExtension => Path.GetExtension(FilePath).ToLowerInvariant();
    public double SizeInMB => SizeInBytes / (1024.0 * 1024.0);
    public bool IsEncrypted { get; init; }
    public bool IsCorrupted { get; init; }
    
    public bool CanProcess() => 
        !IsCorrupted && 
        SizeInBytes > 0 && 
        PageCount > 0 &&
        File.Exists(FilePath);
}
