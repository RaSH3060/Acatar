using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace StreamAvatar.UI.Controls
{
    /// <summary>
    /// Список слоев аватара с возможностью перетаскивания и управления порядком
    /// </summary>
    public class LayerListControl : ListBox
    {
        private Core.Models.AvatarPreset _preset;
        private bool _isDragging;
        private int _dragStartIndex = -1;
        
        public event EventHandler<int> OnLayerSelected;
        public event EventHandler<(int oldIndex, int newIndex)> OnLayerReordered;

        public LayerListControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.UserPaint, true);
            
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 40;
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;
            BorderStyle = BorderStyle.None;
            
            AllowDrop = true;
            
            SelectedIndexChanged += (s, e) =>
            {
                if (SelectedIndex >= 0)
                    OnLayerSelected?.Invoke(this, SelectedIndex);
            };
        }

        public void SetPreset(Core.Models.AvatarPreset preset)
        {
            _preset = preset;
            UpdateItems();
        }

        private void UpdateItems()
        {
            Items.Clear();
            if (_preset != null)
            {
                foreach (var layer in _preset.Layers)
                {
                    Items.Add(layer);
                }
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);
            
            if (e.Index < 0 || _preset == null) return;
            
            var layer = _preset.Layers[e.Index];
            var bounds = e.Bounds;
            
            // Фон
            using (var bgBrush = new SolidBrush((e.State & DrawItemState.Selected) != 0 
                ? Color.FromArgb(60, 60, 60) 
                : Color.FromArgb(45, 45, 45)))
            {
                e.Graphics.FillRectangle(bgBrush, bounds);
            }
            
            // Превью цвета слоя
            var previewRect = new Rectangle(bounds.X + 5, bounds.Y + 5, 30, bounds.Height - 10);
            using (var previewBrush = new SolidBrush(Color.FromArgb(layer.Color)))
            {
                e.Graphics.FillRectangle(previewBrush, previewRect);
            }
            
            // Название слоя
            var textRect = new Rectangle(bounds.X + 40, bounds.Y, bounds.Width - 80, bounds.Height);
            TextRenderer.DrawText(e.Graphics, layer.Name, Font, textRect, Color.White, 
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            
            // Индикатор видимости
            var visibilityRect = new Rectangle(bounds.Right - 30, bounds.Y + 10, 20, 20);
            using (var visBrush = new SolidBrush(layer.Visible ? Color.Lime : Color.Gray))
            {
                e.Graphics.FillEllipse(visBrush, visibilityRect);
            }
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)))
            {
                int oldIndex = (int)e.Data.GetData(typeof(int));
                Point pt = PointToClient(new Point(e.X, e.Y));
                int newIndex = IndexFromPoint(pt);
                
                if (newIndex < 0 || newIndex >= _preset.Layers.Count)
                    newIndex = _preset.Layers.Count - 1;
                
                if (oldIndex != newIndex && oldIndex >= 0 && newIndex >= 0)
                {
                    var layer = _preset.Layers[oldIndex];
                    _preset.Layers.RemoveAt(oldIndex);
                    _preset.Layers.Insert(newIndex, layer);
                    
                    UpdateItems();
                    SelectedIndex = newIndex;
                    
                    OnLayerReordered?.Invoke(this, (oldIndex, newIndex));
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            int index = IndexFromPoint(e.Location);
            if (index >= 0 && e.Button == MouseButtons.Left)
            {
                _dragStartIndex = index;
                _isDragging = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_isDragging && _dragStartIndex >= 0)
            {
                if (Math.Abs(e.X - Location.X) > SystemInformation.DragSize.Width / 2 ||
                    Math.Abs(e.Y - Location.Y) > SystemInformation.DragSize.Height / 2)
                {
                    DoDragDrop(_dragStartIndex, DragDropEffects.Move);
                    _isDragging = false;
                    _dragStartIndex = -1;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            _dragStartIndex = -1;
        }

        public void AddNewLayer()
        {
            if (_preset == null) return;
            
            var newLayer = new Core.Models.AvatarLayer
            {
                Name = $"Layer {_preset.Layers.Count + 1}",
                Position = new Point(100, 100),
                Size = new Size(100, 100),
                Color = Color.Gray.ToArgb(),
                Visible = true
            };
            
            _preset.Layers.Add(newLayer);
            UpdateItems();
            SelectedIndex = Items.Count - 1;
        }

        public void RemoveSelectedLayer()
        {
            if (SelectedIndex < 0 || _preset == null) return;
            
            _preset.Layers.RemoveAt(SelectedIndex);
            UpdateItems();
            
            if (Items.Count > 0)
                SelectedIndex = Math.Min(SelectedIndex, Items.Count - 1);
        }

        public void ToggleLayerVisibility(int index)
        {
            if (index < 0 || index >= _preset.Layers.Count) return;
            
            _preset.Layers[index].Visible = !_preset.Layers[index].Visible;
            Invalidate();
        }
    }
}
