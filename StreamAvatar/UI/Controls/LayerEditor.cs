using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using StreamAvatar.Core;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Редактор слоя аватара с визуальным перетаскиванием
    /// </summary>
    public class LayerEditor : Panel
    {
        private AvatarLayer _layer;
        private bool _isDragging;
        private bool _isResizing;
        private Point _lastMousePos;
        private ResizeHandle _currentHandle;
        
        public event EventHandler<AvatarLayer> OnLayerChanged;
        
        public LayerEditor()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
            
            MinimumSize = new Size(200, 200);
            BackColor = Color.FromArgb(40, 40, 40);
            AutoScroll = true;
        }

        public void SetLayer(AvatarLayer layer)
        {
            _layer = layer;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (_layer == null)
            {
                DrawPlaceholder(e.Graphics);
                return;
            }
            
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Рисуем сетку
            DrawGrid(g);
            
            // Рисуем слой
            DrawLayer(g);
            
            // Рисуем ручки изменения размера
            DrawResizeHandles(g);
        }

        private void DrawGrid(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                for (int x = 0; x < Width; x += 50)
                    g.DrawLine(pen, x, 0, x, Height);
                
                for (int y = 0; y < Height; y += 50)
                    g.DrawLine(pen, 0, y, Width, y);
            }
        }

        private void DrawLayer(Graphics g)
        {
            var rect = new Rectangle(_layer.Position.X, _layer.Position.Y, 
                                     _layer.Size.Width, _layer.Size.Height);
            
            // Если есть изображение - рисуем его
            if (!string.IsNullOrEmpty(_layer.ImagePath) && System.IO.File.Exists(_layer.ImagePath))
            {
                try
                {
                    using (var img = Image.FromFile(_layer.ImagePath))
                    {
                        g.DrawImage(img, rect);
                    }
                }
                catch
                {
                    DrawPlaceholderRect(g, rect, _layer.Name);
                }
            }
            else
            {
                DrawPlaceholderRect(g, rect, _layer.Name);
            }
            
            // Рамка выделения
            using (var pen = new Pen(Color.Cyan, 2))
            {
                g.DrawRectangle(pen, rect);
            }
        }

        private void DrawPlaceholderRect(Graphics g, Rectangle rect, string text)
        {
            using (var brush = new SolidBrush(Color.FromArgb(100, _layer.Color)))
            using (var pen = new Pen(Color.White, 2))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
                
                if (!string.IsNullOrEmpty(text))
                {
                    g.DrawString(text, Font, Brushes.White, rect.X + 5, rect.Y + 5);
                }
            }
        }

        private void DrawResizeHandles(Graphics g)
        {
            var rect = new Rectangle(_layer.Position.X, _layer.Position.Y,
                                     _layer.Size.Width, _layer.Size.Height);
            
            int handleSize = 8;
            Color handleColor = Color.Yellow;
            
            using (var brush = new SolidBrush(handleColor))
            {
                // Углы
                DrawHandle(g, brush, rect.Left - handleSize/2, rect.Top - handleSize/2, handleSize, ResizeHandle.TopLeft);
                DrawHandle(g, brush, rect.Right - handleSize/2, rect.Top - handleSize/2, handleSize, ResizeHandle.TopRight);
                DrawHandle(g, brush, rect.Left - handleSize/2, rect.Bottom - handleSize/2, handleSize, ResizeHandle.BottomLeft);
                DrawHandle(g, brush, rect.Right - handleSize/2, rect.Bottom - handleSize/2, handleSize, ResizeHandle.BottomRight);
                
                // Стороны
                DrawHandle(g, brush, rect.Left + rect.Width/2 - handleSize/2, rect.Top - handleSize/2, handleSize, ResizeHandle.Top);
                DrawHandle(g, brush, rect.Left + rect.Width/2 - handleSize/2, rect.Bottom - handleSize/2, handleSize, ResizeHandle.Bottom);
                DrawHandle(g, brush, rect.Left - handleSize/2, rect.Top + rect.Height/2 - handleSize/2, handleSize, ResizeHandle.Left);
                DrawHandle(g, brush, rect.Right - handleSize/2, rect.Top + rect.Height/2 - handleSize/2, handleSize, ResizeHandle.Right);
            }
        }

        private void DrawHandle(Graphics g, SolidBrush brush, int x, int y, int size, ResizeHandle handle)
        {
            g.FillEllipse(brush, x, y, size, size);
        }

        private void DrawPlaceholder(Graphics g)
        {
            using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
            using (var pen = new Pen(Color.Gray, 2))
            {
                var rect = new Rectangle(20, 20, Width - 40, Height - 40);
                g.FillRoundedRectangle(brush, rect, 10);
                g.DrawRoundedRectangle(pen, rect, 10);
                
                var text = "Выберите слой для редактирования";
                var size = g.MeasureString(text, Font);
                g.DrawString(text, Font, Brushes.Gray,
                    (Width - size.Width) / 2, (Height - size.Height) / 2);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            if (_layer == null) return;
            
            var rect = new Rectangle(_layer.Position.X, _layer.Position.Y,
                                     _layer.Size.Width, _layer.Size.Height);
            
            // Проверяем ручки изменения размера
            _currentHandle = GetHandleAt(e.Location, rect);
            if (_currentHandle != ResizeHandle.None)
            {
                _isResizing = true;
                _lastMousePos = e.Location;
                Cursor = GetCursorForHandle(_currentHandle);
                return;
            }
            
            // Проверяем попадание в прямоугольник
            if (rect.Contains(e.Location))
            {
                _isDragging = true;
                _lastMousePos = e.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_layer == null) return;
            
            var rect = new Rectangle(_layer.Position.X, _layer.Position.Y,
                                     _layer.Size.Width, _layer.Size.Height);
            
            // Обновляем курсор при наведении
            if (!_isDragging && !_isResizing)
            {
                var handle = GetHandleAt(e.Location, rect);
                Cursor = handle != ResizeHandle.None ? GetCursorForHandle(handle) : Cursors.Default;
            }
            
            if (_isDragging)
            {
                var delta = new Size(e.X - _lastMousePos.X, e.Y - _lastMousePos.Y);
                _layer.Position = new Point(_layer.Position.X + delta.Width, _layer.Position.Y + delta.Height);
                _lastMousePos = e.Location;
                OnLayerChanged?.Invoke(this, _layer);
                Invalidate();
            }
            else if (_isResizing)
            {
                ResizeLayer(e.Location);
                OnLayerChanged?.Invoke(this, _layer);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            _isResizing = false;
            _currentHandle = ResizeHandle.None;
            Cursor = Cursors.Default;
        }

        private ResizeHandle GetHandleAt(Point location, Rectangle rect)
        {
            int handleSize = 12;
            var handles = new[]
            {
                new { Handle = ResizeHandle.TopLeft, X = rect.Left, Y = rect.Top },
                new { Handle = ResizeHandle.TopRight, X = rect.Right, Y = rect.Top },
                new { Handle = ResizeHandle.BottomLeft, X = rect.Left, Y = rect.Bottom },
                new { Handle = ResizeHandle.BottomRight, X = rect.Right, Y = rect.Bottom },
                new { Handle = ResizeHandle.Top, X = rect.Left + rect.Width/2, Y = rect.Top },
                new { Handle = ResizeHandle.Bottom, X = rect.Left + rect.Width/2, Y = rect.Bottom },
                new { Handle = ResizeHandle.Left, X = rect.Left, Y = rect.Top + rect.Height/2 },
                new { Handle = ResizeHandle.Right, X = rect.Right, Y = rect.Top + rect.Height/2 }
            };
            
            foreach (var h in handles)
            {
                var handleRect = new Rectangle(h.X - handleSize/2, h.Y - handleSize/2, handleSize, handleSize);
                if (handleRect.Contains(location))
                    return h.Handle;
            }
            
            return ResizeHandle.None;
        }

        private void ResizeLayer(Point location)
        {
            var delta = new Size(location.X - _lastMousePos.X, location.Y - _lastMousePos.Y);
            
            switch (_currentHandle)
            {
                case ResizeHandle.TopLeft:
                    _layer.Position = new Point(_layer.Position.X + delta.Width, _layer.Position.Y + delta.Height);
                    _layer.Size = new Size(_layer.Size.Width - delta.Width, _layer.Size.Height - delta.Height);
                    break;
                case ResizeHandle.TopRight:
                    _layer.Position = new Point(_layer.Position.X, _layer.Position.Y + delta.Height);
                    _layer.Size = new Size(_layer.Size.Width + delta.Width, _layer.Size.Height - delta.Height);
                    break;
                case ResizeHandle.BottomLeft:
                    _layer.Position = new Point(_layer.Position.X + delta.Width, _layer.Position.Y);
                    _layer.Size = new Size(_layer.Size.Width - delta.Width, _layer.Size.Height + delta.Height);
                    break;
                case ResizeHandle.BottomRight:
                    _layer.Size = new Size(_layer.Size.Width + delta.Width, _layer.Size.Height + delta.Height);
                    break;
                case ResizeHandle.Top:
                    _layer.Position = new Point(_layer.Position.X, _layer.Position.Y + delta.Height);
                    _layer.Size = new Size(_layer.Size.Width, _layer.Size.Height - delta.Height);
                    break;
                case ResizeHandle.Bottom:
                    _layer.Size = new Size(_layer.Size.Width, _layer.Size.Height + delta.Height);
                    break;
                case ResizeHandle.Left:
                    _layer.Position = new Point(_layer.Position.X + delta.Width, _layer.Position.Y);
                    _layer.Size = new Size(_layer.Size.Width - delta.Width, _layer.Size.Height);
                    break;
                case ResizeHandle.Right:
                    _layer.Size = new Size(_layer.Size.Width + delta.Width, _layer.Size.Height);
                    break;
            }
            
            // Ограничиваем минимальный размер
            _layer.Size = new Size(Math.Max(10, _layer.Size.Width), Math.Max(10, _layer.Size.Height));
            
            _lastMousePos = location;
        }

        private Cursor GetCursorForHandle(ResizeHandle handle)
        {
            return handle switch
            {
                ResizeHandle.TopLeft => Cursors.SizeNWSE,
                ResizeHandle.TopRight => Cursors.SizeNESW,
                ResizeHandle.BottomLeft => Cursors.SizeNESW,
                ResizeHandle.BottomRight => Cursors.SizeNWSE,
                ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
                ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
                _ => Cursors.Default
            };
        }
    }

    public enum ResizeHandle
    {
        None,
        TopLeft, TopRight, BottomLeft, BottomRight,
        Top, Bottom, Left, Right
    }
}
