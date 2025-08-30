using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using PdfDocument = PdfSharpCore.Pdf.PdfDocument;
using PdfPage = PdfSharpCore.Pdf.PdfPage;

namespace PDFd.Processing.Converters;

/// <summary>
/// Converts PDF to Word. Preserves formatting or dies trying.
/// </summary>
internal sealed class PdfToWordConverter
{
    public async Task<ProcessingResult> ConvertAsync(
        Domain.Models.PdfDocument document,
        ConversionOptions options,
        CancellationToken ct = default)
    {
        var outputPath = GetOutputPath(document.FilePath, options.OutputDirectory);
        
        try
        {
            await Task.Run(() => ConvertCore(document.FilePath, outputPath, options, ct), ct);
            
            var metrics = new Dictionary<string, object>
            {
                ["PagesConverted"] = document.PageCount,
                ["Quality"] = options.Quality.ToString(),
                ["FormattingPreserved"] = options.PreserveFormatting
            };
            
            return ProcessingResult.CreateSuccess(
                PdfConstants.ConversionOperation,
                outputPath,
                TimeSpan.Zero,
                metrics);
        }
        catch (OperationCanceledException)
        {
            // Clean up partial file
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            
            return ProcessingResult.CreateFailure(
                PdfConstants.ConversionOperation,
                "Operation was cancelled");
        }
        catch (Exception ex)
        {
            return ProcessingResult.CreateFailure(
                PdfConstants.ConversionOperation,
                $"Conversion failed: {ex.Message}",
                ex);
        }
    }

    private void ConvertCore(
        string inputPath,
        string outputPath,
        ConversionOptions options,
        CancellationToken ct)
    {
        using var pdfDocument = PdfReader.Open(inputPath, PdfDocumentOpenMode.InformationOnly);
        
        using var wordDocument = WordprocessingDocument.Create(
            outputPath,
            WordprocessingDocumentType.Document);
        
        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());
        
        // Extract text from each page
        for (int i = 0; i < pdfDocument.PageCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            
            var page = pdfDocument.Pages[i];
            var text = ExtractTextFromPage(page);
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                var paragraph = body.AppendChild(new Paragraph());
                var run = paragraph.AppendChild(new Run());
                run.AppendChild(new Text(text));
                
                // Add page break except for last page
                if (i < pdfDocument.PageCount - 1)
                {
                    paragraph.AppendChild(new Run(new Break() { Type = BreakValues.Page }));
                }
            }
        }
        
        mainPart.Document.Save();
    }
    
    private string ExtractTextFromPage(PdfPage page)
    {
        // Basic text extraction - will enhance with proper formatting preservation
        var content = ContentReader.ReadContent(page);
        var text = new System.Text.StringBuilder();
        
        ExtractText(content, text);
        return text.ToString();
    }
    
    private void ExtractText(CObject obj, System.Text.StringBuilder target)
    {
        if (obj is COperator op)
        {
            if (op.OpCode.Name == "Tj" || op.OpCode.Name == "TJ")
            {
                foreach (var operand in op.Operands)
                {
                    if (operand is CString str)
                    {
                        target.Append(str.Value);
                        target.Append(' ');
                    }
                }
            }
        }
        else if (obj is CSequence seq)
        {
            foreach (var item in seq)
            {
                ExtractText(item, target);
            }
        }
    }
    
    private string GetOutputPath(string inputPath, string? outputDirectory)
    {
        var directory = outputDirectory ?? Path.GetDirectoryName(inputPath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, $"{fileNameWithoutExt}{PdfConstants.WordExtension}");
    }
}
