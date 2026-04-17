using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using TearOffTabs.Services;
using TearOffTabs.ViewModels;

namespace TearOffTabs.Views.Controls
{
    public partial class TabShell : UserControl
    {
        private const double DragThreshold    = 6.0;
        private const double TearOffThreshold = 20.0;

        private TabItemViewModel? _draggingTab;
        private Point             _dragStartPosition;
        private TornWindow?       _tornWindow;
        private int               _insertIndex = -1;
        private GhostWindow?      _ghostWindow;
        // Пустой шелл-источник — закроем его окно после drop
        private TabShellViewModel? _emptySourceShell;

        private record TabSlot(TabItemViewModel Tab, double Left, double Mid, double Right);
        private List<TabSlot> _slots   = new();
        private double        _tabWidth;

        // Кешированные ссылки на контролы — ищем один раз при AttachedToVisualTree
        private ItemsControl? _tabStrip;
        private Canvas?       _indicatorCanvas;
        private Rectangle?    _insertIndicator;

        public TabShell()
        {
            InitializeComponent();
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _tabStrip        = this.FindControl<ItemsControl>("TabStrip");
            _indicatorCanvas = this.FindControl<Canvas>("IndicatorCanvas");
            _insertIndicator = this.FindControl<Rectangle>("InsertIndicator");

            if (_tabStrip is null) return;

            _tabStrip.AddHandler(PointerPressedEvent,  OnTabStripPointerPressed,  handledEventsToo: true);
            _tabStrip.AddHandler(PointerMovedEvent,    OnTabStripPointerMoved,    handledEventsToo: true);
            _tabStrip.AddHandler(PointerReleasedEvent, OnTabStripPointerReleased, handledEventsToo: true);

            SubscribeToShell(DataContext as TabShellViewModel);

            // Переподписываемся если DataContext сменился (например в TornWindow)
            DataContextChanged += (_, _) => SubscribeToShell(DataContext as TabShellViewModel);
        }

        private void SubscribeToShell(TabShellViewModel? shell)
        {
            if (shell is null) return;
            shell.Tabs.CollectionChanged += (_, _) =>
            {
                if (shell.Tabs.Count == 0 && _draggingTab is null && _tornWindow is null)
                    WindowManager.Instance.CloseEmptyWindows();
            };
        }

        // ── Pointer ───────────────────────────────────────────────────────────

        private void OnTabStripPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is not TabShellViewModel shell) return;
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
            if (_tabStrip is null) return;

            // Если клик попал на кнопку закрытия — не начинаем drag
            if (e.Source is Button || (e.Source as Visual)?.FindAncestorOfType<Button>() is not null)
                return;

            var pressPos = e.GetPosition(_tabStrip);
            var tab = HitTestTab(pressPos, shell);
            if (tab is null) return;

            shell.SelectedTab  = tab;
            _draggingTab       = tab;
            _dragStartPosition = pressPos;
            _tornWindow        = null;
            _emptySourceShell  = null;
            _insertIndex       = shell.Tabs.IndexOf(tab);

            TakeSnapshot();
            e.Pointer.Capture(_tabStrip);
        }

        private void OnTabStripPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggingTab is null) return;
            if (DataContext is not TabShellViewModel shell) return;
            if (_tabStrip is null) return;

            var pos   = e.GetPosition(_tabStrip);
            var delta = pos - _dragStartPosition;

            // Если окно уже оторвано — двигаем его за курсором
            if (_tornWindow is not null)
            {
                var screenPos = _tabStrip.PointToScreen(pos);

                var snapTarget = WindowManager.Instance.FindSnapTarget(screenPos, _tornWindow);
                if (snapTarget is not null)
                {
                    if (_tornWindow.DataContext is TabShellViewModel tornShell)
                    {
                        var tabs = tornShell.Tabs.ToArray();
                        foreach (var t in tabs)
                            tornShell.TransferTab(t, snapTarget);
                    }

                    _tornWindow  = null;
                    _draggingTab = null;
                    _ghostWindow?.StopTracking();
                    _ghostWindow = null;
                    WindowManager.Instance.CloseEmptyWindows();
                    e.Pointer.Capture(null);
                    return;
                }

                _tornWindow.Position = new PixelPoint(
                    screenPos.X - (int)(_tabWidth / 2),
                    screenPos.Y - 18);

                _ghostWindow?.UpdatePosition(screenPos);
                return;
            }

            if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
                return;

            var cursorScreen = _tabStrip.PointToScreen(pos);

            if (_ghostWindow is null)
            {
                _ghostWindow = new GhostWindow();
                _ghostWindow.StartTracking(_draggingTab.Title, cursorScreen);
            }
            else
            {
                _ghostWindow.UpdatePosition(cursorScreen);
            }

            var outsideVertically = pos.Y > _tabStrip.Bounds.Height + TearOffThreshold
                                 || pos.Y < -TearOffThreshold;

            if (outsideVertically)
            {
                // ── Tear-off ──────────────────────────────────────────────────
                HideInsertIndicator();

                var windowPos = new PixelPoint(
                    cursorScreen.X - (int)(_tabWidth / 2),
                    cursorScreen.Y - 18);

                _tornWindow = WindowManager.Instance.CreateWindow(_draggingTab, shell, windowPos);

                if (shell.Tabs.Count == 0)
                    _emptySourceShell = shell;
            }
            else
            {
                // ── Reorder preview ───────────────────────────────────────────
                var others = GetOtherSlots();
                _insertIndex = GetInsertIndex(pos.X, shell, others);
                ShowInsertIndicator(_insertIndex, shell, others);
            }
        }

        private void OnTabStripPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            HideInsertIndicator();

            _ghostWindow?.StopTracking();
            _ghostWindow = null;
            _tornWindow  = null;

            if (_draggingTab is not null
                && DataContext is TabShellViewModel shell
                && _insertIndex >= 0)
            {
                var currentIndex = shell.Tabs.IndexOf(_draggingTab);
                if (_insertIndex != currentIndex)
                    shell.MoveTab(_draggingTab, _insertIndex);
            }

            // Закрываем пустое окно-источник
            if (_emptySourceShell?.Tabs.Count == 0)
            {
                var sourceWindow = this.GetVisualRoot() as Window;
                sourceWindow?.Close();
            }
            _emptySourceShell = null;

            WindowManager.Instance.CloseEmptyWindows();

            _draggingTab = null;
            _insertIndex = -1;
            _slots.Clear();
            e.Pointer.Capture(null);
        }

        // ── Insert indicator ──────────────────────────────────────────────────

        private void ShowInsertIndicator(int insertIndex, TabShellViewModel shell, List<TabSlot> others)
        {
            if (_indicatorCanvas is null || _insertIndicator is null || _tabStrip is null) return;
            if (others.Count == 0) return;

            var dragOrigIdx = _slots.FindIndex(s => s.Tab == _draggingTab);

            var insertTab  = insertIndex >= 0 && insertIndex < shell.Tabs.Count
                ? shell.Tabs[insertIndex] : null;
            var insertSlot = insertTab is not null
                ? others.FirstOrDefault(s => s.Tab == insertTab) : null;

            double indicatorX;
            if (insertSlot is null)
            {
                indicatorX = others[^1].Right;
            }
            else
            {
                var insertOrigIdx = _slots.FindIndex(s => s.Tab == insertTab);
                indicatorX = dragOrigIdx < insertOrigIdx
                    ? insertSlot.Right
                    : insertSlot.Left;
            }

            _insertIndicator.Height = _tabStrip.Bounds.Height - 4;
            Canvas.SetLeft(_insertIndicator, indicatorX - 1.5);
            Canvas.SetTop(_insertIndicator, 2);
            _indicatorCanvas.IsVisible = true;
        }

        private void HideInsertIndicator()
        {
            if (_indicatorCanvas is not null)
                _indicatorCanvas.IsVisible = false;
        }

        // ── Snapshot ──────────────────────────────────────────────────────────

        private void TakeSnapshot()
        {
            _slots.Clear();
            _tabWidth = 0;
            if (_tabStrip is null) return;

            foreach (var child in _tabStrip.GetVisualDescendants().OfType<Border>())
            {
                if (child.Name != "TabHeader") continue;
                if (child.DataContext is not TabItemViewModel tab) continue;

                var topLeft = child.TranslatePoint(new Point(0, 0), _tabStrip);
                if (!topLeft.HasValue) continue;

                if (_tabWidth == 0) _tabWidth = child.Bounds.Width;
                _slots.Add(new TabSlot(
                    tab,
                    topLeft.Value.X,
                    topLeft.Value.X + child.Bounds.Width / 2,
                    topLeft.Value.X + child.Bounds.Width));
            }

            _slots.Sort((a, b) => a.Left.CompareTo(b.Left));
        }

        // Вычисляется один раз и передаётся в GetInsertIndex + ShowInsertIndicator
        private List<TabSlot> GetOtherSlots() =>
            _slots.Where(s => s.Tab != _draggingTab).ToList();

        private int GetInsertIndex(double cursorX, TabShellViewModel shell, List<TabSlot> others)
        {
            if (others.Count == 0) return 0;

            if (cursorX <= others[0].Mid)
                return shell.Tabs.IndexOf(others[0].Tab);

            if (cursorX >= others[^1].Mid)
                return shell.Tabs.IndexOf(others[^1].Tab);

            for (int i = 0; i < others.Count - 1; i++)
            {
                if (cursorX >= others[i].Mid && cursorX < others[i + 1].Mid)
                {
                    var boundary = (others[i].Mid + others[i + 1].Mid) / 2;
                    return cursorX < boundary
                        ? shell.Tabs.IndexOf(others[i].Tab)
                        : shell.Tabs.IndexOf(others[i + 1].Tab);
                }
            }

            return shell.Tabs.IndexOf(_draggingTab!);
        }

        // ── Hit testing ───────────────────────────────────────────────────────

        private TabItemViewModel? HitTestTab(Point posInTabStrip, TabShellViewModel shell)
        {
            if (_tabStrip is null) return null;

            foreach (var child in _tabStrip.GetVisualDescendants().OfType<Border>())
            {
                if (child.Name != "TabHeader") continue;
                if (child.DataContext is not TabItemViewModel tab) continue;

                var local = _tabStrip.TranslatePoint(posInTabStrip, child);
                if (local.HasValue && new Rect(child.Bounds.Size).Contains(local.Value))
                    return tab;
            }

            if (shell.Tabs.Count == 0) return null;
            var w     = _tabStrip.Bounds.Width / shell.Tabs.Count;
            var index = Math.Clamp((int)(posInTabStrip.X / w), 0, shell.Tabs.Count - 1);
            return shell.Tabs[index];
        }
    }
}
