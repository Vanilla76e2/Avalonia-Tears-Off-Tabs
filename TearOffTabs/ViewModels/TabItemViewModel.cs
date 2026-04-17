using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TearOffTabs.ViewModels
{
    public partial class TabItemViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private ViewModelBase _content = null!;

        [ObservableProperty]
        private bool _isSelected;

        // Владелец вкладки — шелл, в котором она сейчас находится
        public TabShellViewModel? Owner { get; set; }

        public TabItemViewModel(string title, ViewModelBase content)
        {
            _title = title;
            _content = content;
        }

        [RelayCommand]
        private void Close()
        {
            Owner?.CloseTab(this);
        }
    }
}
