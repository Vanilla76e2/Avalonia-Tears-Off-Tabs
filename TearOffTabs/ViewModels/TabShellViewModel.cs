using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace TearOffTabs.ViewModels
{
    public partial class TabShellViewModel : ViewModelBase
    {
        [ObservableProperty]
        private TabItemViewModel? _selectedTab;

        public ObservableCollection<TabItemViewModel> Tabs { get; } = [];

        partial void OnSelectedTabChanged(TabItemViewModel? oldValue, TabItemViewModel? newValue)
        {
            if (oldValue is not null) oldValue.IsSelected = false;
            if (newValue is not null) newValue.IsSelected = true;
        }

        public void AddTab(TabItemViewModel tab)
        {
            tab.Owner = this;
            Tabs.Add(tab);
            SelectedTab = tab;
        }

        public void CloseTab(TabItemViewModel tab)
        {
            var index = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
            tab.Owner = null;

            if (Tabs.Count > 0)
                SelectedTab = Tabs[Math.Max(0, index - 1)];
            else
                SelectedTab = null;
        }

        /// <summary>
        /// Перемещает вкладку на новую позицию внутри этого шелла (reorder).
        /// </summary>
        public void MoveTab(TabItemViewModel tab, int newIndex)
        {
            var currentIndex = Tabs.IndexOf(tab);
            if (currentIndex < 0 || currentIndex == newIndex) return;

            newIndex = Math.Clamp(newIndex, 0, Tabs.Count - 1);
            Tabs.Move(currentIndex, newIndex);
        }

        /// <summary>
        /// Перемещает вкладку из этого шелла в другой.
        /// </summary>
        public void TransferTab(TabItemViewModel tab, TabShellViewModel target)
        {
            if (tab.Owner != this) return;

            var index = Tabs.IndexOf(tab);
            Tabs.Remove(tab);

            if (Tabs.Count > 0)
                SelectedTab = Tabs[Math.Max(0, index - 1)];
            else
                SelectedTab = null;

            target.AddTab(tab);
        }
    }
}
