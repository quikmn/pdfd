// File 4: PDFd.Core/Services/OcrService.cs (basic implementation)
using System.Threading.Tasks;
using PDFd.Core.Interfaces;

namespace PDFd.Core.Services
{
    public class OcrService : IOcrService
    {
        public async Task<string> ExtractTextAsync(string pdfPath)
        {
            // TODO: Implement OCR with Tesseract.NET
            // For now, return empty to allow compilation
            await Task.Delay(1);
            return string.Empty;
        }
    }
}