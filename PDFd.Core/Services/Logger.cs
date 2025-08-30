using System.IO;
using System.Text;

namespace PDFd.Core.Services;

public static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PDFd",
        "pdfd.log");
    
    private static readonly object _lock = new object();
    
    static Logger()
    {
        var dir = Path.GetDirectoryName(LogPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
    
    public static void Log(string message, LogLevel level = LogLevel.Info)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().PadRight(5);
            var logLine = $"[{timestamp}] [{levelStr}] {message}";
            
            File.AppendAllText(LogPath, logLine + Environment.NewLine);
            
            // Also write to console for debugging
            Console.WriteLine(logLine);
        }
    }
    
    public static void LogError(string message, Exception? ex = null)
    {
        var fullMessage = message;
        if (ex != null)
        {
            fullMessage += $"\nException: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        }
        Log(fullMessage, LogLevel.Error);
    }
    
    public static string GetLogPath() => LogPath;
    
    public static string ReadLog()
    {
        if (File.Exists(LogPath))
            return File.ReadAllText(LogPath);
        return "No log file found.";
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}
