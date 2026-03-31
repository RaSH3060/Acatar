using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StreamAvatar.Core;
using StreamAvatar.Rendering;
using StreamAvatar.Audio;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Панель предпросмотра аватара с интерактивным рендерингом
    /// </summary>
    public class AvatarPreviewPanel : Panel
    {
        private AvatarPreset _currentPreset;
        private AvatarRenderer _renderer;
        private AnimationController _animationController;
        private Timer _renderTimer;
        private bool _isDragging;
        private Point _lastMousePos;
        private float _zoom = 1.0f;
        private Point _offset = new Point(0, 0);

        public event EventHandler<float> OnZoomChanged;
        public event EventHandler<Point> OnOffsetChanged;

        public AvatarPreviewPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
            
            MinimumSize = new Size(400, 400);
            BackColor = Color.FromArgb(30, 30, 30);
            
            _renderTimer = new Timer { Interval = 16 }; // ~60 FPS
            _renderTimer.Tick += (s, e) => Invalidate();
            _renderTimer.Start();
            
            MouseWheel += AvatarPreviewPanel_MouseWheel;
            MouseDown += AvatarPreviewPanel_MouseDown;
            MouseMove += AvatarPreviewPanel_MouseMove;
            MouseUp += AvatarPreviewPanel_MouseUp;
        }

        public void SetPreset(AvatarPreset preset, AudioEngine audioEngine)
        {
            _currentPreset = preset;
            
            if (_renderer != null)
                _renderer.Dispose();
            
            _renderer = new Rendering.AvatarRenderer(preset);
            _animationController = new Rendering.AnimationController(preset, audioEngine);
        }

        public void UpdateAudioData(float[] audioData)
        {
            _animationController?.UpdateAudioData(audioData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (_renderer == null || _currentPreset == null)
            {
                DrawPlaceholder(e.Graphics);
                return;
            }

            using (var surface = SkiaSharp.SKSurface.Create(
                new SkiaSharp.GRContext(null),
                new SkiaSharp.GRBackendRenderTarget(Width, Height, 0, 8,
                    new SkiaSharp.GRGlFramebufferInfo(0, SkiaSharp.GRPixelConfig.Rgba8888.ToGlFormat())),
                SkiaSharp.GRSurfaceOrigin.BottomLeft,
                SkiaSharp.SKImageInfo.PlatformColorType))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SkiaSharp.SKColors.Transparent);
                
                // Применяем трансформации
                canvas.Translate(_offset.X + Width / 2, _offset.Y + Height / 2);
                canvas.Scale(_zoom, _zoom);
                canvas.Translate(-Width / 2, -Height / 2);
                
                // Обновляем анимацию
                _animationController?.Update((float)(DateTime.Now.Ticks / 10000000.0));
                
                // Рендерим аватар
                _renderer.Render(canvas, _animationController);
            }
            
            // Fallback для GDI+ если Skia не доступен
            DrawWithGdi(e.Graphics);
        }

        private void DrawWithGdi(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            
            g.TranslateTransform(_offset.X + Width / 2, _offset.Y + Height / 2);
            g.ScaleTransform(_zoom, _zoom);
            g.TranslateTransform(-Width / 2, -Height / 2);
            
            if (_currentPreset != null && _currentPreset.Layers.Count > 0)
            {
                foreach (var layer in _currentPreset.Layers)
                {
                    if (!string.IsNullOrEmpty(layer.ImagePath) && System.IO.File.Exists(layer.ImagePath))
                    {
                        try
                        {
                            using (var img = Image.FromFile(layer.ImagePath))
                            {
                                var rect = new Rectangle(
                                    layer.Position.X, layer.Position.Y,
                                    layer.Size.Width, layer.Size.Height);
                                
                                // Применяем трансформации костей
                                if (layer.Bone != null)
                                {
                                    var transform = _animationController?.GetBoneTransform(layer.Bone) ?? Matrix.Identity;
                                    g.Transform = transform;
                                }
                                
                                g.DrawImage(img, rect);
                            }
                        }
                        catch { /* Игнорируем ошибки загрузки */ }
                    }
                    else
                    {
                        // Рисуем плейсхолдер
                        var rect = new Rectangle(layer.Position.X, layer.Position.Y, layer.Size.Width, layer.Size.Height);
                        using (var brush = new SolidBrush(Color.FromArgb(100, layer.Color)))
                        using (var pen = new Pen(Color.White, 2))
                        {
                            g.FillRectangle(brush, rect);
                            g.DrawRectangle(pen, rect);
                            g.DrawString(layer.Name, Font, Brushes.White, rect.X + 5, rect.Y + 5);
                        }
                    }
                }
            }
        }

        private void DrawPlaceholder(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
            using (var pen = new Pen(Color.FromArgb(100, 100, 100), 2))
            {
                var rect = new Rectangle(50, 50, Width - 100, Height - 100);
                g.FillEllipse(brush, rect);
                g.DrawEllipse(pen, rect);
                
                var text = "Перетащите аватар или загрузите пресет";
                var size = g.MeasureString(text, Font);
                g.DrawString(text, Font, Brushes.Gray, 
                    (Width - size.Width) / 2, (Height - size.Height) / 2);
            }
        }

        private void AvatarPreviewPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            float delta = e.Delta > 0 ? 0.1f : -0.1f;
            _zoom = Math.Max(0.1f, Math.Min(5.0f, _zoom + delta));
            OnZoomChanged?.Invoke(this, _zoom);
            Invalidate();
        }

        private void AvatarPreviewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Control))
            {
                _isDragging = true;
                _lastMousePos = e.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        private void AvatarPreviewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var delta = new Point(e.X - _lastMousePos.X, e.Y - _lastMousePos.Y);
                _offset = new Point(_offset.X + delta.X, _offset.Y + delta.Y);
                _lastMousePos = e.Location;
                OnOffsetChanged?.Invoke(this, _offset);
                Invalidate();
            }
        }

        private void AvatarPreviewPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            Cursor = Cursors.Default;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderTimer?.Dispose();
                _renderer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
