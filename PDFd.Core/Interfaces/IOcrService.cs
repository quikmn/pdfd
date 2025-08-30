// File 2: PDFd.Core/Interfaces/IOcrService.cs
using System.Threading.Tasks;

namespace PDFd.Core.Interfaces
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(string pdfPath);
    }
}