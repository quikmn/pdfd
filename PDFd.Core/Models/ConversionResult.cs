// File 3: PDFd.Core/Models/ConversionResult.cs
using System;

namespace PDFd.Core.Models
{
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public bool UsedOcr { get; set; }
        public int PageCount { get; set; }
    }
}