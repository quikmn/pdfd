using System.Windows;

namespace PDFd.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
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
}
