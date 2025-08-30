using PDFd.Core.Interfaces;
using PDFd.Core.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PDFd.Core.Services
{
    public class ConversionService : IConversionService
    {
        private readonly IXpdfToolsService _xpdfTools;
        private readonly IOcrService _ocrService;

        // UPDATED CONSTRUCTOR - Now takes IXpdfToolsService
        public ConversionService(IXpdfToolsService xpdfTools, IOcrService ocrService)
        {
            _xpdfTools = xpdfTools;
            _ocrService = ocrService;
        }

        public async Task<ConversionResult> ConvertToWordAsync(string inputPath, string outputPath)
        {
            try
            {
                // Step 1: Analyze PDF with pdfinfo
                var pdfInfo = await _xpdfTools.GetPdfInfoAsync(inputPath);
                
                if (pdfInfo.IsEncrypted)
                {
                    return new ConversionResult
                    {
                        Success = false,
                        ErrorMessage = "Cannot convert encrypted PDFs. Please decrypt first.",
                        InputFile = inputPath
                    };
                }

                // Step 2: Extract text with pdftotext
                var extractedText = await _xpdfTools.ExtractTextAsync(inputPath);
                
                // Step 3: If text extraction yields little content, it might be scanned
                bool usedOcr = false;
                if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Length < 100)
                {
                    // Fall back to OCR
                    extractedText = await _ocrService.ExtractTextAsync(inputPath);
                    usedOcr = true;
                }

                // Step 4: Create Word document
                CreateWordDocument(extractedText, outputPath, pdfInfo);

                return new ConversionResult
                {
                    Success = true,
                    InputFile = inputPath,
                    OutputFile = outputPath,
                    ProcessingTime = TimeSpan.Zero, // You can add timing logic
                    UsedOcr = usedOcr,
                    PageCount = pdfInfo.PageCount
                };
            }
            catch (Exception ex)
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    InputFile = inputPath
                };
            }
        }

        private void CreateWordDocument(string text, string outputPath, PdfInfo pdfInfo)
        {
            using (var wordDocument = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Add metadata if available
                if (!string.IsNullOrEmpty(pdfInfo.Title))
                {
                    var titlePara = body.AppendChild(new Paragraph());
                    var titleRun = titlePara.AppendChild(new Run());
                    titleRun.AppendChild(new RunProperties(new Bold()));
                    titleRun.AppendChild(new Text(pdfInfo.Title));
                    body.AppendChild(new Paragraph()); // Empty line
                }

                // Split text into paragraphs
                var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var paragraphText in paragraphs)
                {
                    var para = body.AppendChild(new Paragraph());
                    var run = para.AppendChild(new Run());
                    run.AppendChild(new Text(paragraphText.Trim()));
                }

                mainPart.Document.Save();
            }
        }
    }
}