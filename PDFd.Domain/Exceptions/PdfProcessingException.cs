namespace PDFd.Domain.Exceptions;

public sealed class PdfProcessingException : Exception
{
    public string FilePath { get; }
    public string Operation { get; }
    public ProcessingErrorType ErrorType { get; }
    
    public PdfProcessingException(
        string message,
        string filePath,
        string operation,
        ProcessingErrorType errorType,
        Exception? innerException = null) 
        : base(message, innerException)
    {
        FilePath = filePath;
        Operation = operation;
        ErrorType = errorType;
    }
}

public enum ProcessingErrorType
{
    FileNotFound,
    FileCorrupted,
    FileEncrypted,
    InsufficientMemory,
    PermissionDenied,
    UnsupportedFormat,
    Unknown
}
