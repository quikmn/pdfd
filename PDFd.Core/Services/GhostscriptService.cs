using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PDFd.Core.Services
{
    public interface IGhostscriptService
    {
        Task<bool> CompressPdfAsync(string inputPath, string outputPath, CompressionLevel level = CompressionLevel.Medium);
        bool IsGhostscriptAvailable();
    }

    public enum CompressionLevel
    {
        Low,    // High quality, larger file
        Medium, // Balanced
        High    // Lower quality, smaller file
    }

    public class GhostscriptService : IGhostscriptService
    {
        private readonly string _gsPath;
        
        public GhostscriptService()
        {
            _gsPath = FindGhostscript();
        }

        public bool IsGhostscriptAvailable() => !string.IsNullOrEmpty(_gsPath);

        public async Task<bool> CompressPdfAsync(string inputPath, string outputPath, CompressionLevel level = CompressionLevel.Medium)
        {
            if (!IsGhostscriptAvailable())
                throw new InvalidOperationException("Ghostscript not found. Please install Ghostscript or use the bundled version.");

            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"Input PDF not found: {inputPath}");

            var settings = GetCompressionSettings(level);
            
            var args = $"-sDEVICE=pdfwrite " +
                      $"-dCompatibilityLevel=1.4 " +
                      $"-dPDFSETTINGS={settings} " +
                      $"-dNOPAUSE -dBATCH -dQUIET " +
                      $"-sOutputFile=\"{outputPath}\" " +
                      $"\"{inputPath}\"";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _gsPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0 && File.Exists(outputPath);
        }

        private string GetCompressionSettings(CompressionLevel level)
        {
            return level switch
            {
                CompressionLevel.Low => "/prepress",    // 300 dpi, high quality
                CompressionLevel.Medium => "/ebook",    // 150 dpi, good quality
                CompressionLevel.High => "/screen",     // 72 dpi, low quality
                _ => "/ebook"
            };
        }

        private string FindGhostscript()
        {
            // 1. Check bundled version first (in Tools folder)
            var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Ghostscript", "gswin64c.exe");
            if (File.Exists(bundledPath))
                return bundledPath;

            // 2. Check common installation paths
            var commonPaths = new[]
            {
                @"C:\Program Files\gs\gs10.02.1\bin\gswin64c.exe",
                @"C:\Program Files\gs\gs10.02.0\bin\gswin64c.exe",
                @"C:\Program Files\gs\gs10.01.0\bin\gswin64c.exe",
                @"C:\Program Files\gs\gs9.56.1\bin\gswin64c.exe",
                @"C:\Program Files (x86)\gs\gs9.56.1\bin\gswin32c.exe"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // 3. Try to find any version
            var gsRoot = @"C:\Program Files\gs";
            if (Directory.Exists(gsRoot))
            {
                var gsExe = Directory.GetFiles(gsRoot, "gswin64c.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrEmpty(gsExe))
                    return gsExe;
            }

            return null;
        }
    }
}