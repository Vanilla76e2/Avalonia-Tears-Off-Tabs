using CommunityToolkit.Mvvm.ComponentModel;

namespace TearOffTabs.ViewModels.Pages
{
    public partial class ReportsPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _message = "Reports Page — drag me anywhere!";
    }
}
