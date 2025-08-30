using System.Threading.Tasks;

namespace PDFd.Core.Interfaces
{
    public interface IXpdfToolsService
    {
        Task<string> ExtractTextAsync(string pdfPath, int? startPage = null, int? endPage = null);
        Task<PdfInfo> GetPdfInfoAsync(string pdfPath);
        Task<string> ExtractImagesAsync(string pdfPath, string outputDirectory);
        Task<string> ConvertToHtmlAsync(string pdfPath, string outputPath);
    }

    public class PdfInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public int PageCount { get; set; }
        public bool IsEncrypted { get; set; }
        public string FileSize { get; set; }
        public string PdfVersion { get; set; }
    }
}