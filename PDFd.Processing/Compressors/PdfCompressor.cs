using System.IO;

namespace PDFd.Processing.Compressors;

/// <summary>
/// Temporary compressor - just copies the file until we add Ghostscript
/// </summary>
internal sealed class PdfCompressor
{
    public async Task<ProcessingResult> CompressAsync(
        Domain.Models.PdfDocument document,
        CompressionOptions options,
        CancellationToken ct = default)
    {
        var outputPath = GetCompressedOutputPath(document.FilePath);
        
        try
        {
            // For now, just copy the file
            await Task.Run(() => File.Copy(document.FilePath, outputPath, true), ct);
            
            var metrics = new Dictionary<string, object>
            {
                ["OriginalSizeBytes"] = document.SizeInBytes,
                ["CompressedSizeBytes"] = document.SizeInBytes,
                ["CompressionRatio"] = 0,
                ["Method"] = "Copy (compression not implemented yet)"
            };
            
            return ProcessingResult.CreateSuccess(
                PdfConstants.CompressionOperation,
                outputPath,
                TimeSpan.Zero,
                metrics);
        }
        catch (Exception ex)
        {
            return ProcessingResult.CreateFailure(
                PdfConstants.CompressionOperation,
                $"Compression failed: {ex.Message}",
                ex);
        }
    }
    
    private string GetCompressedOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, $"{fileNameWithoutExt}_compressed.pdf");
    }
}
