using CommunityToolkit.Mvvm.ComponentModel;

namespace PDFd.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "PDF'd - Professional PDF Processing";
    }
}
