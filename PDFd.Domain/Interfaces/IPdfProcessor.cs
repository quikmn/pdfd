using PDFd.Domain.Models;

namespace PDFd.Domain.Interfaces;

public interface IPdfProcessor
{
    Task<ProcessingResult> ConvertToWordAsync(
        PdfDocument document,
        ConversionOptions options,
        CancellationToken ct = default);
    
    Task<ProcessingResult> CompressAsync(
        PdfDocument document,
        CompressionOptions options,
        CancellationToken ct = default);
    
    Task<bool> ValidateAsync(
        string filePath,
        CancellationToken ct = default);
    
    Task<PdfDocument?> GetMetadataAsync(
        string filePath,
        CancellationToken ct = default);
}
