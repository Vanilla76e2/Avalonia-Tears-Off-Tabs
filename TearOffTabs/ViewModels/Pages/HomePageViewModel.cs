using CommunityToolkit.Mvvm.ComponentModel;

namespace TearOffTabs.ViewModels.Pages
{
    public partial class HomePageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _message = "Home Page — drag this tab out of the window!";
    }
}
