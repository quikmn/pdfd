namespace PDFd.Domain.Constants;

public static class PdfConstants
{
    public const string PdfExtension = ".pdf";
    public const string WordExtension = ".docx";
    
    public const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500MB
    public const int MaxPageCount = 10000;
    public const int MaxBatchSize = 10000;
    
    public const string ConversionOperation = "PDF_TO_WORD";
    public const string CompressionOperation = "COMPRESS";
    public const string BatchOperation = "BATCH";
}

public static class PerformanceTargets
{
    public const int MaxStartupMs = 1000;
    public const int MaxMemoryMB = 500;
    public const double MinProcessingMBPerSecond = 10.0;
    public const double MinSuccessRate = 0.99;
}
