using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Editor
{
    public class GridOverlay : UserControl
    {
        public int GridSize { get; set; } = 16;
        public Brush GridLineBrush { get; set; } = Brushes.LightGray;
        public double GridLineThickness { get; set; } = 1.0;

        public GridOverlay()
        {
            this.IsHitTestVisible = false; // Let mouse events pass through
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            int width = (int)ActualWidth;
            int height = (int)ActualHeight;
            var pen = new Pen(GridLineBrush, GridLineThickness);
            for (int x = 0; x < width; x += GridSize)
                dc.DrawLine(pen, new Point(x, 0), new Point(x, height));
            for (int y = 0; y < height; y += GridSize)
                dc.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }
} 