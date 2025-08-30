// File 1: PDFd.Core/Interfaces/IConversionService.cs
using System.Threading.Tasks;
using PDFd.Core.Models;

namespace PDFd.Core.Interfaces
{
    public interface IConversionService
    {
        Task<ConversionResult> ConvertToWordAsync(string inputPath, string outputPath);
    }
}