using System;
using System.Drawing;
using System.Windows.Forms;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Визуальный редактор костей скелета аватара
    /// </summary>
    public class BoneEditor : Panel
    {
        private Core.Models.AvatarPreset _preset;
        private Core.Models.Bone _selectedBone;
        private bool _isDragging;
        private Point _lastMousePos;
        
        public event EventHandler<Core.Models.Bone> OnBoneSelected;
        public event EventHandler<Core.Models.Bone> OnBoneMoved;

        public BoneEditor()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
            
            MinimumSize = new Size(300, 300);
            BackColor = Color.FromArgb(35, 35, 35);
        }

        public void SetPreset(Core.Models.AvatarPreset preset)
        {
            _preset = preset;
            Invalidate();
        }

        public void SelectBone(Core.Models.Bone bone)
        {
            _selectedBone = bone;
            OnBoneSelected?.Invoke(this, bone);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (_preset == null || _preset.Bones.Count == 0)
            {
                DrawPlaceholder(e.Graphics);
                return;
            }
            
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Рисуем сетку
            DrawGrid(g);
            
            // Рисуем кости
            foreach (var bone in _preset.Bones)
            {
                DrawBone(g, bone);
            }
        }

        private void DrawGrid(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(50, 50, 50), 1))
            {
                for (int x = 0; x < Width; x += 50)
                    g.DrawLine(pen, x, 0, x, Height);
                
                for (int y = 0; y < Height; y += 50)
                    g.DrawLine(pen, 0, y, Width, y);
            }
        }

        private void DrawBone(Graphics g, Core.Models.Bone bone)
        {
            // Рисуем связь с родительской костью
            if (bone.Parent != null)
            {
                using (var pen = new Pen(Color.Gray, 2))
                {
                    g.DrawLine(pen, bone.Position, bone.Parent.Position);
                }
            }
            
            // Рисуем саму кость
            int radius = (_selectedBone == bone) ? 12 : 8;
            Color color = (_selectedBone == bone) ? Color.Cyan : Color.LightBlue;
            
            using (var brush = new SolidBrush(color))
            using (var pen = new Pen(Color.White, 2))
            {
                g.FillEllipse(brush, bone.Position.X - radius, bone.Position.Y - radius, radius * 2, radius * 2);
                g.DrawEllipse(pen, bone.Position.X - radius, bone.Position.Y - radius, radius * 2, radius * 2);
                
                // Подпись
                if (!string.IsNullOrEmpty(bone.Name))
                {
                    g.DrawString(bone.Name, Font, Brushes.White, 
                        bone.Position.X + 15, bone.Position.Y - 10);
                }
            }
        }

        private void DrawPlaceholder(Graphics g)
        {
            using (var brush = new SolidBrush(Color.FromArgb(40, 40, 40)))
            using (var pen = new Pen(Color.Gray, 2))
            {
                var rect = new Rectangle(50, 50, Width - 100, Height - 100);
                g.FillRoundedRectangle(brush, rect, 10);
                g.DrawRoundedRectangle(pen, rect, 10);
                
                var text = "Добавьте кости для скелетной анимации";
                var size = g.MeasureString(text, Font);
                g.DrawString(text, Font, Brushes.Gray,
                    (Width - size.Width) / 2, (Height - size.Height) / 2);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            if (_preset == null) return;
            
            // Ищем кость под курсором
            foreach (var bone in _preset.Bones)
            {
                int distance = (int)Math.Sqrt(
                    Math.Pow(e.X - bone.Position.X, 2) + 
                    Math.Pow(e.Y - bone.Position.Y, 2));
                
                if (distance <= 15)
                {
                    _selectedBone = bone;
                    _isDragging = true;
                    _lastMousePos = e.Location;
                    Cursor = Cursors.Hand;
                    OnBoneSelected?.Invoke(this, bone);
                    Invalidate();
                    return;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_preset == null) return;
            
            // Обновляем курсор при наведении
            if (!_isDragging)
            {
                bool overBone = false;
                foreach (var bone in _preset.Bones)
                {
                    int distance = (int)Math.Sqrt(
                        Math.Pow(e.X - bone.Position.X, 2) + 
                        Math.Pow(e.Y - bone.Position.Y, 2));
                    
                    if (distance <= 15)
                    {
                        overBone = true;
                        break;
                    }
                }
                Cursor = overBone ? Cursors.Hand : Cursors.Default;
            }
            
            if (_isDragging && _selectedBone != null)
            {
                var delta = new Point(e.X - _lastMousePos.X, e.Y - _lastMousePos.Y);
                _selectedBone.Position = new Point(
                    _selectedBone.Position.X + delta.X,
                    _selectedBone.Position.Y + delta.Y);
                
                _lastMousePos = e.Location;
                OnBoneMoved?.Invoke(this, _selectedBone);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            Cursor = Cursors.Default;
        }

        public void AddBone(string name, Point position, Core.Models.Bone parent = null)
        {
            if (_preset == null)
                _preset = new Core.Models.AvatarPreset();
            
            var bone = new Core.Models.Bone
            {
                Name = name,
                Position = position,
                Parent = parent,
                Rotation = 0,
                Length = 50
            };
            
            _preset.Bones.Add(bone);
            SelectBone(bone);
        }

        public void RemoveSelectedBone()
        {
            if (_selectedBone != null && _preset != null)
            {
                _preset.Bones.Remove(_selectedBone);
                _selectedBone = null;
                Invalidate();
            }
        }
    }
}
