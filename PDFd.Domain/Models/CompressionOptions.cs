namespace PDFd.Domain.Models;

public sealed record CompressionOptions
{
    public required CompressionLevel Level { get; init; }
    public required int TargetDpi { get; init; }
    public required bool CompressImages { get; init; }
    public required bool RemoveMetadata { get; init; }
    public long? TargetSizeBytes { get; init; }
    
    public static CompressionOptions Balanced => new()
    {
        Level = CompressionLevel.Medium,
        TargetDpi = 150,
        CompressImages = true,
        RemoveMetadata = false
    };
    
    public static CompressionOptions Maximum => new()
    {
        Level = CompressionLevel.Maximum,
        TargetDpi = 96,
        CompressImages = true,
        RemoveMetadata = true
    };
    
    public static CompressionOptions Minimal => new()
    {
        Level = CompressionLevel.Low,
        TargetDpi = 300,
        CompressImages = false,
        RemoveMetadata = false
    };
}

public enum CompressionLevel
{
    None,
    Low,
    Medium,
    High,
    Maximum
}
