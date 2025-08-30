using Microsoft.Extensions.DependencyInjection;
using PDFd.Core.Interfaces;
using PDFd.Core.Services;
using PDFd.UI.ViewModels;
using PDFd.UI.Views;
using System;
using System.Windows;

namespace PDFd.UI
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        public static string? CommandLineOutputFolder { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core PDF Processing Services
            services.AddSingleton<IXpdfToolsService, XpdfToolsService>();
            services.AddSingleton<IConversionService, ConversionService>();
            services.AddSingleton<IOcrService, OcrService>();
            services.AddSingleton<IGhostscriptService, GhostscriptService>();
            services.AddSingleton<IPdfIntelligenceService, PdfIntelligenceService>();
            
            // ViewModels
            services.AddTransient<MainViewModel>();
            
            // Views
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}