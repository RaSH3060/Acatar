using System;
using System.Drawing;
using System.Windows.Forms;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Круговой контроллер для управления движением глаз
    /// </summary>
    public class EyeMovementController : Panel
    {
        private float _eyeX;
        private float _eyeY;
        private float _radius = 50;
        private bool _isDragging;
        
        public event EventHandler<(float x, float y)> OnEyePositionChanged;
        
        public float EyeX
        {
            get => _eyeX;
            set
            {
                _eyeX = Math.Max(-1, Math.Min(1, value));
                Invalidate();
            }
        }
        
        public float EyeY
        {
            get => _eyeY;
            set
            {
                _eyeY = Math.Max(-1, Math.Min(1, value));
                Invalidate();
            }
        }
        
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = Math.Max(10, Math.Min(200, value));
                Invalidate();
            }
        }

        public EyeMovementController()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
            
            MinimumSize = new Size(150, 150);
            BackColor = Color.FromArgb(30, 30, 30);
            Size = new Size(200, 200);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            int centerX = Width / 2;
            int centerY = Height / 2;
            
            // Рисуем внешнюю окружность
            using (var pen = new Pen(Color.Gray, 2))
            {
                g.DrawEllipse(pen, centerX - _radius, centerY - _radius, _radius * 2, _radius * 2);
            }
            
            // Рисуем сетку
            DrawGrid(g, centerX, centerY);
            
            // Рисуем текущую позицию глаз
            int eyeX = centerX + (int)(_eyeX * _radius);
            int eyeY = centerY + (int)(_eyeY * _radius);
            
            using (var brush = new SolidBrush(Color.Cyan))
            {
                g.FillEllipse(brush, eyeX - 8, eyeY - 8, 16, 16);
            }
            
            using (var pen = new Pen(Color.White, 2))
            {
                g.DrawEllipse(pen, eyeX - 8, eyeY - 8, 16, 16);
            }
            
            // Подпись координат
            string coords = $"X: {_eyeX:F2}, Y: {_eyeY:F2}";
            g.DrawString(coords, Font, Brushes.White, 10, 10);
        }

        private void DrawGrid(Graphics g, int centerX, int centerY)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                // Горизонтальная линия
                g.DrawLine(pen, centerX - _radius, centerY, centerX + _radius, centerY);
                
                // Вертикальная линия
                g.DrawLine(pen, centerX, centerY - _radius, centerX, centerY + _radius);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            int centerX = Width / 2;
            int centerY = Height / 2;
            
            double distance = Math.Sqrt(
                Math.Pow(e.X - centerX, 2) + 
                Math.Pow(e.Y - centerY, 2));
            
            if (distance <= _radius + 10)
            {
                _isDragging = true;
                UpdateEyePosition(e.Location, centerX, centerY);
                Cursor = Cursors.Cross;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_isDragging)
            {
                int centerX = Width / 2;
                int centerY = Height / 2;
                UpdateEyePosition(e.Location, centerX, centerY);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            Cursor = Cursors.Default;
        }

        private void UpdateEyePosition(Point location, int centerX, int centerY)
        {
            float dx = location.X - centerX;
            float dy = location.Y - centerY;
            
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance > _radius)
            {
                float ratio = _radius / (float)distance;
                dx *= ratio;
                dy *= ratio;
            }
            
            _eyeX = dx / _radius;
            _eyeY = dy / _radius;
            
            OnEyePositionChanged?.Invoke(this, (_eyeX, _eyeY));
            Invalidate();
        }

        public void ResetPosition()
        {
            _eyeX = 0;
            _eyeY = 0;
            Invalidate();
        }
    }
}
