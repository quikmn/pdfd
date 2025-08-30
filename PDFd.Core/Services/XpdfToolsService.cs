// PDFd.Core/Services/XpdfToolsService.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PDFd.Core.Interfaces;

namespace PDFd.Core.Services
{
    // Model to hold PDF info
    public class XpdfToolsService : IXpdfToolsService
    {
        private readonly string _toolsPath;
        
        public XpdfToolsService()
        {
            _toolsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Xpdf");
            
            if (!Directory.Exists(_toolsPath))
            {
                throw new DirectoryNotFoundException($"Xpdf tools not found at: {_toolsPath}");
            }
        }

        public async Task<string> ExtractTextAsync(string pdfPath, int? startPage = null, int? endPage = null)
        {
            ValidatePdfPath(pdfPath);
            
            var args = new StringBuilder($"\"{pdfPath}\" -");
            
            if (startPage.HasValue)
                args.Append($" -f {startPage.Value}");
            
            if (endPage.HasValue)
                args.Append($" -l {endPage.Value}");
            
            args.Append(" -enc UTF-8");
            
            var result = await RunToolAsync("pdftotext.exe", args.ToString());
            return result.Output;
        }

        public async Task<PdfInfo> GetPdfInfoAsync(string pdfPath)
        {
            ValidatePdfPath(pdfPath);
            
            var result = await RunToolAsync("pdfinfo.exe", $"\"{pdfPath}\"");
            
            return ParsePdfInfo(result.Output);
        }

        public async Task<string> ExtractImagesAsync(string pdfPath, string outputDirectory)
        {
            ValidatePdfPath(pdfPath);
            
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            
            var outputPrefix = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(pdfPath));
            
            var args = $"\"{pdfPath}\" \"{outputPrefix}\"";
            var result = await RunToolAsync("pdfimages.exe", args);
            
            return outputDirectory;
        }

        public async Task<string> ConvertToHtmlAsync(string pdfPath, string outputPath)
        {
            ValidatePdfPath(pdfPath);
            
            var args = $"\"{pdfPath}\" \"{outputPath}\"";
            await RunToolAsync("pdftohtml.exe", args);
            
            return outputPath;
        }

        private async Task<ToolResult> RunToolAsync(string toolName, string arguments)
        {
            var toolPath = Path.Combine(_toolsPath, toolName);
            
            if (!File.Exists(toolPath))
                throw new FileNotFoundException($"Tool not found: {toolPath}");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = errorBuilder.ToString();
                if (!string.IsNullOrWhiteSpace(error))
                    throw new InvalidOperationException($"{toolName} failed: {error}");
            }

            return new ToolResult
            {
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString(),
                ExitCode = process.ExitCode
            };
        }

        private void ValidatePdfPath(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("PDF path cannot be empty", nameof(pdfPath));
            
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");
            
            if (!Path.GetExtension(pdfPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File must be a PDF", nameof(pdfPath));
        }

        private PdfInfo ParsePdfInfo(string output)
        {
            var info = new PdfInfo();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2);
                if (parts.Length != 2) continue;
                
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                
                switch (key)
                {
                    case "Title":
                        info.Title = value;
                        break;
                    case "Author":
                        info.Author = value;
                        break;
                    case "Pages":
                        if (int.TryParse(value, out var pages))
                            info.PageCount = pages;
                        break;
                    case "Encrypted":
                        info.IsEncrypted = value.Contains("yes", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "File size":
                        info.FileSize = value;
                        break;
                    case "PDF version":
                        info.PdfVersion = value;
                        break;
                }
            }
            
            return info;
        }

        private class ToolResult
        {
            public string Output { get; set; }
            public string Error { get; set; }
            public int ExitCode { get; set; }
        }
    }
}