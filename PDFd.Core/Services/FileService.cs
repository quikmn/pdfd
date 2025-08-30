namespace PDFd.Core.Services;

/// <summary>
/// Handles file system operations. Keeps it simple.
/// </summary>
public static class FileService
{
    public static bool IsPdfFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }
    
    public static IEnumerable<string> GetPdfFiles(string directory)
    {
        if (!Directory.Exists(directory)) return Enumerable.Empty<string>();
        
        return Directory.EnumerateFiles(directory, "*.pdf", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).StartsWith("~")) // Skip temp files
            .OrderBy(f => f);
    }
    
    public static string GetSafeOutputPath(string inputPath, string suffix)
    {
        var dir = Path.GetDirectoryName(inputPath) ?? "";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
        var ext = Path.GetExtension(inputPath);
        
        var outputPath = Path.Combine(dir, $"{nameWithoutExt}{suffix}{ext}");
        var counter = 1;
        
        // Ensure we don't overwrite existing files
        while (File.Exists(outputPath))
        {
            outputPath = Path.Combine(dir, $"{nameWithoutExt}{suffix}_{counter}{ext}");
            counter++;
        }
        
        return outputPath;
    }
    
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}
