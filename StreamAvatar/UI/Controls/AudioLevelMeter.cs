using System;
using System.Drawing;
using System.Windows.Forms;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Визуализатор аудио уровня в реальном времени
    /// </summary>
    public class AudioLevelMeter : Panel
    {
        private float _currentLevel;
        private float _peakLevel;
        private DateTime _lastPeakTime;
        private Timer _decayTimer;
        
        public float CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = Math.Max(0, Math.Min(1, value));
                if (_currentLevel > _peakLevel)
                {
                    _peakLevel = _currentLevel;
                    _lastPeakTime = DateTime.Now;
                }
                Invalidate();
            }
        }

        public Color LowColor { get; set; } = Color.FromArgb(0, 255, 100);
        public Color MediumColor { get; set; } = Color.FromArgb(255, 255, 0);
        public Color HighColor { get; set; } = Color.FromArgb(255, 50, 50);
        public bool ShowGrid { get; set; } = true;
        public bool Vertical { get; set; } = false;

        public AudioLevelMeter()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
            
            MinimumSize = new Size(30, 100);
            BackColor = Color.FromArgb(20, 20, 20);
            
            _decayTimer = new Timer { Interval = 50 };
            _decayTimer.Tick += (s, e) =>
            {
                // Плавное затухание пика
                if ((DateTime.Now - _lastPeakTime).TotalMilliseconds > 1000)
                {
                    _peakLevel = Math.Max(0, _peakLevel - 0.02f);
                    Invalidate();
                }
            };
            _decayTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Очистка
            using (var bgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }
            
            // Сетка
            if (ShowGrid)
            {
                DrawGrid(g);
            }
            
            // Основной уровень
            DrawLevel(g);
            
            // Пиковый маркер
            DrawPeakMarker(g);
        }

        private void DrawGrid(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                if (Vertical)
                {
                    for (int i = 1; i < 10; i++)
                    {
                        int y = Height - (i * Height / 10);
                        g.DrawLine(pen, 0, y, Width, y);
                    }
                }
                else
                {
                    for (int i = 1; i < 10; i++)
                    {
                        int x = i * Width / 10;
                        g.DrawLine(pen, x, 0, x, Height);
                    }
                }
            }
        }

        private void DrawLevel(Graphics g)
        {
            int levelSize = Vertical ? (int)(_currentLevel * Height) : (int)(_currentLevel * Width);
            
            // Градиент от зеленого к красному
            Color levelColor;
            if (_currentLevel < 0.5f)
                levelColor = InterpolateColor(LowColor, MediumColor, _currentLevel * 2);
            else if (_currentLevel < 0.8f)
                levelColor = InterpolateColor(MediumColor, HighColor, (_currentLevel - 0.5f) * 3.33f);
            else
                levelColor = HighColor;
            
            using (var brush = new SolidBrush(levelColor))
            {
                if (Vertical)
                {
                    g.FillRectangle(brush, 2, Height - levelSize, Width - 4, levelSize);
                }
                else
                {
                    g.FillRectangle(brush, 2, 2, levelSize, Height - 4);
                }
            }
        }

        private void DrawPeakMarker(Graphics g)
        {
            int peakPos = Vertical ? Height - (int)(_peakLevel * Height) : (int)(_peakLevel * Width);
            
            using (var pen = new Pen(Color.White, 2))
            {
                if (Vertical)
                {
                    g.DrawLine(pen, 0, peakPos, Width, peakPos);
                }
                else
                {
                    g.DrawLine(pen, peakPos, 0, peakPos, Height);
                }
            }
        }

        private Color InterpolateColor(Color c1, Color c2, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)(c1.R + (c2.R - c1.R) * t),
                (int)(c1.G + (c2.G - c1.G) * t),
                (int)(c1.B + (c2.B - c1.B) * t)
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _decayTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
