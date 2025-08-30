using System.Windows;

namespace PDFd.UI;

public partial class App : Application
{
    public static string? CommandLineOutputFolder { get; private set; }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Parse command line arguments
        ParseCommandLineArgs(e.Args);
        
        // Set up any global exception handling here
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show(
                $"An unexpected error occurred: {ex?.Message}",
                "PDF'd Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };
    }
    
    private void ParseCommandLineArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i].Equals("-outputfolder", StringComparison.OrdinalIgnoreCase) ||
                 args[i].Equals("--outputfolder", StringComparison.OrdinalIgnoreCase) ||
                 args[i].Equals("-o", StringComparison.OrdinalIgnoreCase)) 
                && i + 1 < args.Length)
            {
                CommandLineOutputFolder = args[i + 1];
                // Create directory if it doesn't exist
                if (!System.IO.Directory.Exists(CommandLineOutputFolder))
                {
                    System.IO.Directory.CreateDirectory(CommandLineOutputFolder);
                }
            }
        }
    }
}
