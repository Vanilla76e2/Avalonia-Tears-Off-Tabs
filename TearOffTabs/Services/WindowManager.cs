using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;
using TearOffTabs.ViewModels;
using TearOffTabs.Views;
using TearOffTabs.Views.Controls;

namespace TearOffTabs.Services
{
    public class WindowManager
    {
        public static WindowManager Instance { get; } = new();

        private readonly List<TornWindow> _tornWindows = [];

        private WindowManager() { }

        /// <summary>
        /// Создаёт TornWindow с одной вкладкой в указанной позиции экрана.
        /// </summary>
        public TornWindow CreateWindow(TabItemViewModel tab, TabShellViewModel sourceShell, PixelPoint screenPosition)
        {
            var newShell = new TabShellViewModel();

            var tornWindow = new TornWindow
            {
                DataContext = newShell,
                Position    = screenPosition,
            };

            _tornWindows.Add(tornWindow);
            tornWindow.Closed += (_, _) => _tornWindows.Remove(tornWindow);

            sourceShell.TransferTab(tab, newShell);
            tornWindow.Show();

            return tornWindow;
        }

        /// <summary>
        /// Ищет окно (кроме excludeWindow), чей TabStrip содержит указанную экранную точку.
        /// </summary>
        public TabShellViewModel? FindSnapTarget(PixelPoint screenPoint, Window excludeWindow)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow is not null && mainWindow != excludeWindow)
            {
                var shell = FindTabShellAt(mainWindow, screenPoint);
                if (shell is not null) return shell;
            }

            foreach (var w in _tornWindows)
            {
                if (w == excludeWindow) continue;
                var shell = FindTabShellAt(w, screenPoint);
                if (shell is not null) return shell;
            }

            return null;
        }

        /// <summary>
        /// Закрывает все TornWindow с пустым шеллом.
        /// Вызывается после drop и при закрытии вкладки кнопкой ✕.
        /// </summary>
        public void CloseEmptyWindows()
        {
            foreach (var w in _tornWindows.ToArray())
            {
                if (w.DataContext is TabShellViewModel shell && shell.Tabs.Count == 0)
                    w.Close();
            }
        }

        private static TabShellViewModel? FindTabShellAt(Window window, PixelPoint screenPoint)
        {
            var tabShell = window.GetVisualDescendants()
                .OfType<TabShell>()
                .FirstOrDefault();

            if (tabShell?.DataContext is not TabShellViewModel shellVm)
                return null;

            var tabStrip = tabShell.FindControl<ItemsControl>("TabStrip");
            if (tabStrip is null) return null;

            var topLeft     = tabStrip.PointToScreen(new Point(0, 0));
            var bottomRight = tabStrip.PointToScreen(new Point(tabStrip.Bounds.Width, tabStrip.Bounds.Height));

            return screenPoint.X >= topLeft.X     && screenPoint.X <= bottomRight.X
                && screenPoint.Y >= topLeft.Y - 10 && screenPoint.Y <= bottomRight.Y + 20
                ? shellVm : null;
        }

        public static Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }
    }
}
