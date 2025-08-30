using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace PDFd.Processing;

/// <summary>
/// Main PDF processing engine. Does the heavy lifting.
/// </summary>
public sealed class PdfProcessor : IPdfProcessor, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public PdfProcessor(int maxConcurrency = 4)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task<ProcessingResult> ConvertToWordAsync(
        Domain.Models.PdfDocument document,
        ConversionOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        if (!document.CanProcess())
        {
            return ProcessingResult.CreateFailure(
                PdfConstants.ConversionOperation,
                "Document cannot be processed");
        }

        await _semaphore.WaitAsync(ct);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var converter = new Converters.PdfToWordConverter();
            var result = await converter.ConvertAsync(document, options, ct);
            
            stopwatch.Stop();
            return result with { ProcessingTime = stopwatch.Elapsed };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ProcessingResult> CompressAsync(
        Domain.Models.PdfDocument document,
        CompressionOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        await _semaphore.WaitAsync(ct);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var compressor = new Compressors.PdfCompressor();
            var result = await compressor.CompressAsync(document, options, ct);
            
            stopwatch.Stop();
            return result with { ProcessingTime = stopwatch.Elapsed };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ValidateAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return false;

        try
        {
            return await Task.Run(() =>
            {
                using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.InformationOnly);
                return document.PageCount > 0;
            }, ct);
        }
        catch
        {
            return false;
        }
    }

    public async Task<Domain.Models.PdfDocument?> GetMetadataAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            return await Task.Run(() =>
            {
                var fileInfo = new FileInfo(filePath);
                using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.InformationOnly);
                
                return new Domain.Models.PdfDocument
                {
                    FilePath = filePath,
                    SizeInBytes = fileInfo.Length,
                    PageCount = document.PageCount,
                    CreatedAt = fileInfo.CreationTime,
                    ModifiedAt = fileInfo.LastWriteTime,
                    IsEncrypted = document.SecuritySettings.DocumentSecurityLevel != PdfSharpCore.Pdf.Security.PdfDocumentSecurityLevel.None,
                    IsCorrupted = false
                };
            }, ct);
        }
        catch
        {
            // If we can't open it, it's probably corrupted
            var fileInfo = new FileInfo(filePath);
            return new Domain.Models.PdfDocument
            {
                FilePath = filePath,
                SizeInBytes = fileInfo.Length,
                PageCount = 0,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                IsCorrupted = true,
                IsEncrypted = false
            };
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}
