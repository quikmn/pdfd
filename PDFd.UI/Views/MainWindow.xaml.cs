using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using PDFd.Core.Services;
using System.Diagnostics;

namespace PDFd.UI.Views;

public partial class MainWindow : Window
{
    private readonly PdfService _pdfService;
    private readonly List<string> _selectedFiles = new();
    private readonly DispatcherTimer _historyUpdateTimer;
    private string? _customOutputFolder;
    
    public MainWindow()
    {
        InitializeComponent();
        _pdfService = new PdfService();
        
        // Set up timer to update history time labels
        _historyUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _historyUpdateTimer.Tick += (s, e) => UpdateHistoryDisplay();
        _historyUpdateTimer.Start();
        
        // Apply command line output folder if provided
        if (!string.IsNullOrEmpty(App.CommandLineOutputFolder))
        {
            SetOutputFolder(App.CommandLineOutputFolder);
        }
        
        UpdateUI();
        UpdateHistoryDisplay();
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
    
    private void OutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Use a SaveFileDialog as a workaround to select folder
        var dialog = new SaveFileDialog
        {
            Title = "Select output folder",
            Filter = "Folder Selection|*.folder",
            FileName = "Select Folder",
            CheckFileExists = false,
            CheckPathExists = true,
            OverwritePrompt = false
        };
        
        if (!string.IsNullOrEmpty(_customOutputFolder))
        {
            dialog.InitialDirectory = _customOutputFolder;
        }
        
        if (dialog.ShowDialog() == true)
        {
            // Get the directory path from the selected file path
            var folderPath = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(folderPath))
            {
                SetOutputFolder(folderPath);
            }
        }
    }
    
    private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
    {
        _customOutputFolder = null;
        _pdfService.OutputDirectory = null;
        OutputFolderButton.Content = "Same as input";
        OutputFolderButton.ToolTip = "Click to set custom output folder";
        ClearOutputButton.Visibility = Visibility.Collapsed;
    }
    
    private void SetOutputFolder(string folder)
    {
        _customOutputFolder = folder;
        _pdfService.OutputDirectory = folder;
        
        // Show truncated path in button
        var displayPath = folder;
        if (displayPath.Length > 25)
        {
            displayPath = "..." + displayPath.Substring(displayPath.Length - 22);
        }
        
        OutputFolderButton.Content = displayPath;
        OutputFolderButton.ToolTip = $"Output to: {folder}\nClick to change";
        ClearOutputButton.Visibility = Visibility.Visible;
    }
    
    private async void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessButton.IsEnabled = false;
        var originalContent = ProcessButton.Content;
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
            
            var stopwatch = Stopwatch.StartNew();
            var result = await _pdfService.ProcessBatchAsync(
                _selectedFiles,
                operation,
                progress);
            stopwatch.Stop();
            
            // Update history display
            UpdateHistoryDisplay();
            
            // Determine output location for message
            var outputLocation = !string.IsNullOrEmpty(_customOutputFolder) 
                ? _customOutputFolder 
                : Path.GetDirectoryName(_selectedFiles.First());
            
            var message = $"PDF'd Complete!\n\n" +
                         $"✓ Processed: {result.SuccessfulFiles} of {result.TotalFiles} files\n" +
                         $"✓ Time: {stopwatch.Elapsed.TotalSeconds:0.#}s\n" +
                         $"✓ Operation: {(operation == BatchOperationType.ConvertToWord ? "Converted to Word" : "Compressed")}\n" +
                         $"✓ Location: {outputLocation}\n\n" +
                         $"Would you like to open the output folder?";
            
            var openFolder = MessageBox.Show(
                message,
                "PDF'd Complete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            
            if (openFolder == MessageBoxResult.Yes && !string.IsNullOrEmpty(outputLocation))
            {
                Process.Start("explorer.exe", outputLocation);
            }
            
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
            ProcessButton.Content = originalContent;
            UpdateUI();
        }
    }
    
    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _pdfService.ClearHistory();
        UpdateHistoryDisplay();
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
        
        if (_selectedFiles.Any())
        {
            EmptyState.Visibility = Visibility.Collapsed;
            FileListContainer.Visibility = Visibility.Visible;
            
            // Update file list display with better info
            var fileItems = _selectedFiles.Select(f => new
            {
                Name = Path.GetFileName(f),
                Status = FileService.FormatFileSize(new FileInfo(f).Length)
            }).ToList();
            
            FileList.ItemsSource = fileItems;
            
            // Update file count label
            var totalSize = _selectedFiles.Sum(f => new FileInfo(f).Length);
            FileCountLabel.Text = $"{_selectedFiles.Count} files • {FileService.FormatFileSize(totalSize)}";
            ProcessButton.Content = $"Get PDF'd ({_selectedFiles.Count})";
        }
        else
        {
            EmptyState.Visibility = Visibility.Visible;
            FileListContainer.Visibility = Visibility.Collapsed;
            FileCountLabel.Text = "";
            ProcessButton.Content = "Get PDF'd";
        }
    }
    
    private void UpdateHistoryDisplay()
    {
        var history = _pdfService.GetRecentHistory(20).ToList();
        
        if (!history.Any())
        {
            HistoryList.ItemsSource = null;
            HistorySummary.Text = "No recent activity";
            return;
        }
        
        // Create display items with calculated properties
        var historyItems = history.Select(h => new
        {
            h.Status,
            StatusBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(h.StatusColor)),
            h.FileName,
            TimeAgo = GetTimeAgo(h.ProcessedAt),
            Details = GetHistoryDetails(h)
        }).ToList();
        
        HistoryList.ItemsSource = historyItems;
        
        // Update summary
        var successCount = history.Count(h => h.Success);
        var failCount = history.Count(h => !h.Success);
        var todayCount = history.Count(h => h.ProcessedAt.Date == DateTime.Today);
        
        HistorySummary.Text = $"Today: {todayCount} • Success: {successCount} • Failed: {failCount}";
    }
    
    private string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.Now - dateTime;
        
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return dateTime.ToString("MMM d");
    }
    
    private string GetHistoryDetails(Domain.Models.ProcessingHistoryItem item)
    {
        var details = item.Operation;
        
        if (item.FileSizeBytes.HasValue)
        {
            details += $" • {FileService.FormatFileSize(item.FileSizeBytes.Value)}";
        }
        
        if (!item.Success && !string.IsNullOrEmpty(item.ErrorMessage))
        {
            details += $" • {item.ErrorMessage}";
        }
        
        return details;
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _historyUpdateTimer?.Stop();
        _pdfService?.Dispose();
    }
}
