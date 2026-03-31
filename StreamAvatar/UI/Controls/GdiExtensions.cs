using System.Drawing;
using System.Drawing.Drawing2D;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Расширения для GDI+ рисования скругленных прямоугольников
    /// </summary>
    public static class GdiExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (var path = CreateRoundedRectanglePath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = CreateRoundedRectanglePath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            // Верхний левый угол
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            
            // Верхняя сторона
            path.AddLine(rect.X + radius, rect.Y, rect.Right - radius, rect.Y);
            
            // Верхний правый угол
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            
            // Правая сторона
            path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom - radius);
            
            // Нижний правый угол
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            
            // Нижняя сторона
            path.AddLine(rect.Right - radius, rect.Bottom, rect.X + radius, rect.Bottom);
            
            // Нижний левый угол
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            
            // Левая сторона
            path.AddLine(rect.X, rect.Bottom - radius, rect.X, rect.Y + radius);
            
            path.CloseFigure();
            return path;
        }
    }
}
