using System;
using System.Drawing;
using System.Windows.Forms;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Контроллер для настройки параметров анимации с визуальным представлением
    /// </summary>
    public class AnimationParameterSlider : Panel
    {
        private float _value;
        private float _minValue;
        private float _maxValue;
        private bool _isDragging;
        private string _label;
        private string _unit;
        
        public event EventHandler<float> OnValueChanged;
        
        public float Value
        {
            get => _value;
            set
            {
                _value = Math.Max(_minValue, Math.Min(_maxValue, value));
                Invalidate();
            }
        }
        
        public float MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                _value = Math.Max(_minValue, _value);
                Invalidate();
            }
        }
        
        public float MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                _value = Math.Min(_maxValue, _value);
                Invalidate();
            }
        }
        
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                Invalidate();
            }
        }
        
        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                Invalidate();
            }
        }

        public AnimationParameterSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
            
            MinimumSize = new Size(200, 40);
            BackColor = Color.Transparent;
            
            _value = 50;
            _minValue = 0;
            _maxValue = 100;
            _label = "Parameter";
            _unit = "";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            int sliderHeight = 8;
            int handleSize = 16;
            int trackY = Height / 2;
            
            // Рисуем фон трека
            using (var brush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                g.FillRoundedRectangle(brush, 
                    new Rectangle(10, trackY - sliderHeight/2, Width - 20, sliderHeight), 
                    4);
            }
            
            // Вычисляем позицию ползунка
            float normalizedValue = (_value - _minValue) / (_maxValue - _minValue);
            int handleX = 10 + (int)(normalizedValue * (Width - 20 - handleSize));
            
            // Рисуем заполненную часть трека
            using (var brush = new SolidBrush(Color.Cyan))
            {
                g.FillRoundedRectangle(brush,
                    new Rectangle(10, trackY - sliderHeight/2, handleX - 10 + handleSize/2, sliderHeight),
                    4);
            }
            
            // Рисуем ползунок
            using (var brush = new SolidBrush(Color.White))
            using (var pen = new Pen(Color.Cyan, 2))
            {
                g.FillEllipse(brush, handleX, trackY - handleSize/2, handleSize, handleSize);
                g.DrawEllipse(pen, handleX, trackY - handleSize/2, handleSize, handleSize);
            }
            
            // Рисуем метку
            string labelText = $"{_label}: {_value:F1}{_unit}";
            g.DrawString(labelText, Font, Brushes.White, 10, 5);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            int trackY = Height / 2;
            float normalizedValue = (_value - _minValue) / (_maxValue - _minValue);
            int handleX = 10 + (int)(normalizedValue * (Width - 20 - 16));
            
            // Проверяем попадание в ползунок
            if (e.X >= handleX - 8 && e.X <= handleX + 8 &&
                e.Y >= trackY - 8 && e.Y <= trackY + 8)
            {
                _isDragging = true;
                Cursor = Cursors.Hand;
                UpdateValueFromMouse(e.Location);
            }
            else if (e.Y >= trackY - 10 && e.Y <= trackY + 10)
            {
                // Клик в трек - перемещаем ползунок
                _isDragging = true;
                UpdateValueFromMouse(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_isDragging)
            {
                UpdateValueFromMouse(e.Location);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            Cursor = Cursors.Default;
        }

        private void UpdateValueFromMouse(Point location)
        {
            int handleSize = 16;
            float normalizedValue = (float)(location.X - 10) / (Width - 20 - handleSize);
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
            
            _value = _minValue + normalizedValue * (_maxValue - _minValue);
            
            OnValueChanged?.Invoke(this, _value);
            Invalidate();
        }
    }
}
