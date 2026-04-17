using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace TearOffTabs.Views.Controls
{
    /// <summary>
    /// Полупрозрачное окно-призрак, которое следует за курсором во время drag.
    /// Позиция обновляется снаружи через UpdatePosition().
    /// IsHitTestVisible="False" — не мешает DragDrop-событиям.
    /// </summary>
    public partial class GhostWindow : Window
    {
        private const int OffsetX = 12;
        private const int OffsetY = -10;

        public GhostWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Показывает призрак с заданным заголовком в начальной позиции.
        /// </summary>
        public void StartTracking(string title, PixelPoint startScreenPos)
        {
            var titleText = this.FindControl<TextBlock>("TitleText");
            if (titleText is not null)
                titleText.Text = title;

            Position = ToGhostPos(startScreenPos);
            Show();
        }

        /// <summary>
        /// Обновляет позицию призрака. Вызывается из PointerMoved TabShell.
        /// </summary>
        public void UpdatePosition(PixelPoint screenPos)
        {
            Position = ToGhostPos(screenPos);
        }

        /// <summary>
        /// Закрывает призрак.
        /// </summary>
        public void StopTracking()
        {
            Close();
        }

        private static PixelPoint ToGhostPos(PixelPoint screenPos) =>
            new(screenPos.X + OffsetX, screenPos.Y + OffsetY);
    }
}
