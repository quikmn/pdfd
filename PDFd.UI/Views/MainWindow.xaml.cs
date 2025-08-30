using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PDFd.Core.Services;

namespace PDFd.UI.Views;

public partial class MainWindow : Window
{
    private readonly PdfService _pdfService;
    private readonly List<string> _selectedFiles = new();
    
    public MainWindow()
    {
        InitializeComponent();
        _pdfService = new PdfService();
        UpdateUI();
    }
    
    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files.Where(f => FileService.IsPdfFile(f)));
        }
    }
    
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) 
            ? DragDropEffects.Copy 
            : DragDropEffects.None;
        e.Handled = true;
    }
    
    private void DropZone_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF Files|*.pdf",
            Multiselect = true,
            Title = "Select PDFs to process"
        };
        
        if (dialog.ShowDialog() == true)
        {
            AddFiles(dialog.FileNames);
        }
    }
    
    private async void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessButton.IsEnabled = false;
        ProcessButton.Content = "Processing...";
        
        try
        {
            var operation = ConvertOption.IsChecked == true
                ? BatchOperationType.ConvertToWord
                : BatchOperationType.Compress;
            
            var progress = new Progress<Domain.Models.BatchOperation>(batch =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProcessButton.Content = $"Processing... {batch.ProgressPercentage:0}%";
                });
            });
            
            var result = await _pdfService.ProcessBatchAsync(
                _selectedFiles,
                operation,
                progress);
            
            MessageBox.Show(
                $"Processed {result.SuccessfulFiles} of {result.TotalFiles} files successfully!",
                "PDF'd Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            _selectedFiles.Clear();
            UpdateUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error: {ex.Message}",
                "PDF'd Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ProcessButton.Content = "Get PDF'd";
            UpdateUI();
        }
    }
    
    private void AddFiles(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            if (!_selectedFiles.Contains(file))
            {
                _selectedFiles.Add(file);
            }
        }
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        ProcessButton.IsEnabled = _selectedFiles.Any();
        
        // Toggle visibility
        if (_selectedFiles.Any())
        {
            EmptyState.Visibility = Visibility.Collapsed;
            FileListContainer.Visibility = Visibility.Visible;
            
            // Update file list display
            var fileItems = _selectedFiles.Select(f => new
            {
                Name = Path.GetFileName(f),
                Status = "Ready"
            }).ToList();
            
            FileList.ItemsSource = fileItems;
        }
        else
        {
            EmptyState.Visibility = Visibility.Visible;
            FileListContainer.Visibility = Visibility.Collapsed;
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _pdfService?.Dispose();
    }
}
