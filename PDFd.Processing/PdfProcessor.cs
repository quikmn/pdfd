using System.Diagnostics;
using System.Text;

namespace PDFd.Processing;

public sealed class PdfProcessor : IPdfProcessor, IDisposable
{
    private readonly string _toolsPath;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;
    private readonly bool _useExternalTools;

    public PdfProcessor(int maxConcurrency = 4)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        // Try multiple paths to find tools
        var possiblePaths = new[]
{
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Xpdf"),
    Path.Combine(Directory.GetCurrentDirectory(), "Tools", "Xpdf"),
    Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "Tools", "Xpdf"),
    @"D:\Dev\pdfd\PDFd.UI\bin\Debug\net8.0-windows\Tools\Xpdf" // Updated hardcoded fallback
};

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "pdftotext.exe")))
            {
                _toolsPath = path;
                _useExternalTools = true;
                Console.WriteLine($"Found tools at: {path}");
                break;
            }
        }

        if (!_useExternalTools)
        {
            _toolsPath = "";
            Console.WriteLine("Warning: pdftotext.exe not found. Text extraction will be limited.");
        }
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
            var converter = new Converters.PdfToWordConverter(_toolsPath, _useExternalTools);
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

        if (_useExternalTools)
        {
            try
            {
                var pdfInfo = Path.Combine(_toolsPath, "pdfinfo.exe");
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = pdfInfo,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                await Task.Run(() => process!.WaitForExit(), ct);
                return process!.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public async Task<Domain.Models.PdfDocument?> GetMetadataAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var pageCount = 1;
            var isEncrypted = false;

            if (_useExternalTools)
            {
                var pdfInfo = Path.Combine(_toolsPath, "pdfinfo.exe");
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = pdfInfo,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                var output = await process!.StandardOutput.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit(), ct);

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("Pages:"))
                    {
                        var pagesStr = line.Substring(6).Trim();
                        int.TryParse(pagesStr, out pageCount);
                    }
                    else if (line.StartsWith("Encrypted:"))
                    {
                        isEncrypted = !line.Contains("no");
                    }
                }
            }

            return new Domain.Models.PdfDocument
            {
                FilePath = filePath,
                SizeInBytes = fileInfo.Length,
                PageCount = pageCount,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                IsEncrypted = isEncrypted,
                IsCorrupted = false
            };
        }
        catch
        {
            return null;
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
