using CommunityToolkit.Mvvm.ComponentModel;

namespace TearOffTabs.ViewModels.Pages
{
    public partial class SettingsPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _message = "Settings Page — try dragging me to a new window!";
    }
}
