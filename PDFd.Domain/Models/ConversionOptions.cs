namespace PDFd.Domain.Models;

public sealed record ConversionOptions
{
    public required ConversionQuality Quality { get; init; }
    public required bool PreserveFormatting { get; init; }
    public required bool ExtractImages { get; init; }
    public required bool UseOcr { get; init; }
    public string? OutputDirectory { get; init; }
    
    public static ConversionOptions Default => new()
    {
        Quality = ConversionQuality.High,
        PreserveFormatting = true,
        ExtractImages = true,
        UseOcr = false
    };
    
    public static ConversionOptions Fast => new()
    {
        Quality = ConversionQuality.Draft,
        PreserveFormatting = false,
        ExtractImages = false,
        UseOcr = false
    };
}

public enum ConversionQuality
{
    Draft,
    Standard,
    High
}
