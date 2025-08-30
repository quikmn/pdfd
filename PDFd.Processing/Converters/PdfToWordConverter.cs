using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using System.Text;
using PDFd.Processing.Internal;

namespace PDFd.Processing.Converters;

internal sealed class PdfToWordConverter
{
    private readonly string _toolsPath;
    private readonly bool _useExternalTools;
    
    public PdfToWordConverter(string toolsPath, bool useExternalTools)
    {
        _toolsPath = toolsPath;
        _useExternalTools = useExternalTools;
        Logger.Log($"PdfToWordConverter initialized. Tools path: {_toolsPath}, Using external tools: {_useExternalTools}");
    }
    
    public async Task<ProcessingResult> ConvertAsync(
        Domain.Models.PdfDocument document,
        ConversionOptions options,
        CancellationToken ct = default)
    {
        Logger.Log($"Starting conversion for: {document.FilePath}");
        var outputPath = GetOutputPath(document.FilePath, options.OutputDirectory);
        Logger.Log($"Output will be: {outputPath}");
        
        try
        {
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Logger.Log($"Creating output directory: {outputDir}");
                Directory.CreateDirectory(outputDir);
            }
            
            string extractedText;
            
            if (_useExternalTools)
            {
                Logger.Log("Using pdftotext for extraction");
                extractedText = await ExtractWithPdfToText(document.FilePath, ct);
                Logger.Log($"Extracted {extractedText.Length} characters");
            }
            else
            {
                Logger.Log("WARNING: pdftotext not found, using fallback");
                extractedText = "PDF text extraction requires pdftotext.exe tool.\nPlease install from xpdfreader.com";
            }
            
            Logger.Log("Creating Word document");
            CreateWordDocument(outputPath, extractedText);
            Logger.Log($"Conversion successful: {outputPath}");
            
            var metrics = new Dictionary<string, object>
            {
                ["PagesConverted"] = document.PageCount,
                ["Method"] = _useExternalTools ? "pdftotext" : "fallback"
            };
            
            return ProcessingResult.CreateSuccess(
                PdfConstants.ConversionOperation,
                outputPath,
                TimeSpan.Zero,
                metrics);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Conversion failed for {document.FilePath}", ex);
            return ProcessingResult.CreateFailure(
                PdfConstants.ConversionOperation,
                $"Conversion failed: {ex.Message}",
                ex);
        }
    }
    
    private async Task<string> ExtractWithPdfToText(string pdfPath, CancellationToken ct)
    {
        var pdfToText = Path.Combine(_toolsPath, "pdftotext.exe");
        var tempFile = Path.GetTempFileName();
        
        Logger.Log($"Running pdftotext: {pdfToText}");
        Logger.Log($"Input: {pdfPath}");
        Logger.Log($"Temp output: {tempFile}");
        
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = pdfToText,
                Arguments = $"-layout \"{pdfPath}\" \"{tempFile}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            
            await Task.Run(() => process!.WaitForExit(), ct);
            
            if (process!.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                Logger.LogError($"pdftotext failed with exit code {process.ExitCode}: {error}");
                throw new Exception($"pdftotext failed: {error}");
            }
            
            var text = await File.ReadAllTextAsync(tempFile, ct);
            Logger.Log($"Successfully extracted {text.Length} characters");
            return text;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                Logger.Log($"Cleaning up temp file: {tempFile}");
                File.Delete(tempFile);
            }
        }
    }
    
        private void CreateWordDocument(string outputPath, string text)
    {
        Logger.Log($"Creating Word document at: {outputPath}");
        
        // Clean invalid XML characters
        text = CleanForXml(text);
        
        using var wordDocument = WordprocessingDocument.Create(
            outputPath,
            WordprocessingDocumentType.Document);
        
        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());
        
        var lines = text.Split('\n');
        Logger.Log($"Processing {lines.Length} lines");
        
        foreach (var line in lines)
        {
            // Skip empty lines that are just form feeds
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            var paragraph = new Paragraph();
            var run = new Run();
            run.AppendChild(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.AppendChild(run);
            body.AppendChild(paragraph);
        }
        
        mainPart.Document.Save();
        Logger.Log("Word document saved successfully");
    }
    
    private string CleanForXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // Remove or replace invalid XML characters
        var cleaned = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            // Valid XML chars: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD]
            if (c == '\t' || c == '\n' || c == '\r' || 
                (c >= 0x20 && c <= 0xD7FF) || 
                (c >= 0xE000 && c <= 0xFFFD))
            {
                cleaned.Append(c);
            }
            else if (c == '\f') // Form feed - replace with newline
            {
                cleaned.Append('\n');
            }
            // Skip other invalid characters
        }
        
        Logger.Log($"Cleaned text: removed {text.Length - cleaned.Length} invalid characters");
        return cleaned.ToString();
    }private string GetOutputPath(string inputPath, string? outputDirectory)
    {
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
        
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            return Path.Combine(outputDirectory, $"{fileNameWithoutExt}{PdfConstants.WordExtension}");
        }
        else
        {
            var directory = Path.GetDirectoryName(inputPath) ?? "";
            return Path.Combine(directory, $"{fileNameWithoutExt}{PdfConstants.WordExtension}");
        }
    }
}


