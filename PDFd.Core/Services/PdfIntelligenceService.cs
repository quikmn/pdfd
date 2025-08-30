using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFd.Core.Services
{
    public interface IPdfIntelligenceService
    {
        Task<PdfAnalysis> AnalyzePdfAsync(string pdfPath);
    }

    public class PdfAnalysis
    {
        public bool IsValid { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsScanned { get; set; }
        public bool HasText { get; set; }
        public bool HasImages { get; set; }
        public bool HasAttachments { get; set; }
        public int PageCount { get; set; }
        public long FileSizeBytes { get; set; }
        public string PdfVersion { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<FontInfo> Fonts { get; set; } = new();
        public List<string> Attachments { get; set; } = new();
        public string Recommendation { get; set; }
    }

    public class FontInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsEmbedded { get; set; }
        public bool IsSubset { get; set; }
    }

    public class PdfIntelligenceService : IPdfIntelligenceService
    {
        private readonly string _toolsPath;

        public PdfIntelligenceService()
        {
            _toolsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Xpdf");
        }

        public async Task<PdfAnalysis> AnalyzePdfAsync(string pdfPath)
        {
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF not found: {pdfPath}");

            var analysis = new PdfAnalysis
            {
                FileSizeBytes = new FileInfo(pdfPath).Length
            };

            // Run all analysis tools in parallel
            var infoTask = GetPdfInfoAsync(pdfPath);
            var fontsTask = GetPdfFontsAsync(pdfPath);
            var textTask = CheckForTextAsync(pdfPath);
            var imagesTask = CheckForImagesAsync(pdfPath);
            var attachmentsTask = GetAttachmentsAsync(pdfPath);

            await Task.WhenAll(infoTask, fontsTask, textTask, imagesTask, attachmentsTask);

            // Process results
            ProcessPdfInfo(analysis, await infoTask);
            ProcessFonts(analysis, await fontsTask);
            analysis.HasText = await textTask;
            analysis.HasImages = await imagesTask;
            analysis.Attachments = await attachmentsTask;
            analysis.HasAttachments = analysis.Attachments.Any();

            // Determine if scanned
            analysis.IsScanned = !analysis.HasText && analysis.HasImages;

            // Generate recommendation
            GenerateRecommendation(analysis);

            return analysis;
        }

        private async Task<string> GetPdfInfoAsync(string pdfPath)
        {
            return await RunToolAsync("pdfinfo.exe", $"\"{pdfPath}\"");
        }

        private async Task<string> GetPdfFontsAsync(string pdfPath)
        {
            return await RunToolAsync("pdffonts.exe", $"\"{pdfPath}\"");
        }

        private async Task<bool> CheckForTextAsync(string pdfPath)
        {
            var text = await RunToolAsync("pdftotext.exe", $"\"{pdfPath}\" -");
            return !string.IsNullOrWhiteSpace(text) && text.Length > 50;
        }

        private async Task<bool> CheckForImagesAsync(string pdfPath)
        {
            var result = await RunToolAsync("pdfimages.exe", $"-list \"{pdfPath}\"");
            return result.Contains("image");
        }

        private async Task<List<string>> GetAttachmentsAsync(string pdfPath)
        {
            var result = await RunToolAsync("pdfdetach.exe", $"-list \"{pdfPath}\"");
            var attachments = new List<string>();
            
            if (!string.IsNullOrEmpty(result) && !result.Contains("0 embedded files"))
            {
                var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                attachments.AddRange(lines.Where(l => !l.StartsWith("Error")));
            }
            
            return attachments;
        }

        private void ProcessPdfInfo(PdfAnalysis analysis, string infoOutput)
        {
            var lines = infoOutput.Split('\n');
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "Pages":
                        if (int.TryParse(value, out var pages))
                            analysis.PageCount = pages;
                        break;
                    case "Encrypted":
                        analysis.IsEncrypted = value.Contains("yes", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "PDF version":
                        analysis.PdfVersion = value;
                        break;
                    case "Tagged":
                        if (value.Contains("no", StringComparison.OrdinalIgnoreCase))
                            analysis.Issues.Add("PDF is not tagged (accessibility issue)");
                        break;
                }
            }

            analysis.IsValid = analysis.PageCount > 0;
        }

        private void ProcessFonts(PdfAnalysis analysis, string fontsOutput)
        {
            var lines = fontsOutput.Split('\n').Skip(2); // Skip header lines
            
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;

                var font = new FontInfo
                {
                    Name = parts[0],
                    Type = parts[1],
                    IsEmbedded = parts[3].Contains("yes", StringComparison.OrdinalIgnoreCase),
                    IsSubset = parts[4].Contains("yes", StringComparison.OrdinalIgnoreCase)
                };
                
                analysis.Fonts.Add(font);

                if (!font.IsEmbedded)
                {
                    analysis.Issues.Add($"Font '{font.Name}' is not embedded - may display incorrectly");
                }
            }
        }

        private void GenerateRecommendation(PdfAnalysis analysis)
        {
            if (analysis.IsEncrypted)
            {
                analysis.Recommendation = "PDF is encrypted. Decrypt before processing.";
            }
            else if (analysis.IsScanned)
            {
                analysis.Recommendation = "This is a scanned PDF. OCR will be used for text extraction.";
            }
            else if (analysis.Issues.Any(i => i.Contains("Font")))
            {
                analysis.Recommendation = "Font issues detected. Conversion may have formatting problems.";
            }
            else if (!analysis.HasText && !analysis.HasImages)
            {
                analysis.Recommendation = "PDF appears to be empty or corrupted.";
            }
            else
            {
                analysis.Recommendation = "PDF is healthy and ready for processing.";
            }
        }

        private async Task<string> RunToolAsync(string toolName, string arguments)
        {
            var toolPath = Path.Combine(_toolsPath, toolName);
            
            if (!File.Exists(toolPath))
                return string.Empty;

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            return output;
        }
    }
}