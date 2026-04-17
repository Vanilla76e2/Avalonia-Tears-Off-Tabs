using CommunityToolkit.Mvvm.Input;
using TearOffTabs.ViewModels.Pages;

namespace TearOffTabs.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public TabShellViewModel TabShell { get; } = new();

        public MainWindowViewModel()
        {
            // Создаём несколько демо-вкладок
            TabShell.AddTab(new TabItemViewModel("Home", new HomePageViewModel()));
            TabShell.AddTab(new TabItemViewModel("Settings", new SettingsPageViewModel()));
            TabShell.AddTab(new TabItemViewModel("Reports", new ReportsPageViewModel()));
        }

        [RelayCommand]
        private void AddNewTab()
        {
            var count = TabShell.Tabs.Count + 1;
            TabShell.AddTab(new TabItemViewModel($"Tab {count}", new HomePageViewModel
            {
                Message = $"This is dynamically created tab #{count}"
            }));
        }
    }
}
