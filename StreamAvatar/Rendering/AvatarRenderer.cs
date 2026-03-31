using System;
using System.IO;
using SkiaSharp;
using StreamAvatar.Core;

namespace StreamAvatar.Rendering
{
    /// <summary>
    /// Renders the avatar using SkiaSharp with support for layers, bones, and effects
    /// </summary>
    public class AvatarRenderer
    {
        private AvatarPreset? _preset;
        private AnimationController _animationController = new();
        
        // Cached bitmaps
        private Dictionary<string, SKBitmap> _bitmapCache = new();
        
        public int CanvasWidth { get; set; } = 512;
        public int CanvasHeight { get; set; } = 512;
        
        public AnimationController AnimationController => _animationController;
        
        public AvatarPreset? Preset
        {
            get => _preset;
            set
            {
                _preset = value;
                _animationController.Preset = value;
                ClearBitmapCache();
            }
        }
        
        public void Render(SKCanvas canvas)
        {
            if (_preset == null)
            {
                // Clear canvas if no preset
                canvas.Clear(SKColors.Transparent);
                return;
            }
            
            canvas.Clear(SKColors.Transparent);
            
            // Apply shake offset to entire canvas
            var shakeX = _animationController.ShakeOffsetX;
            var shakeY = _animationController.ShakeOffsetY;
            canvas.Translate(shakeX, shakeY);
            
            // Sort layers by order
            var sortedLayers = _preset.Layers.OrderBy(l => l.Order).ToList();
            
            foreach (var layer in sortedLayers)
            {
                if (!layer.Visible) continue;
                
                RenderLayer(canvas, layer);
            }
        }
        
        private void RenderLayer(SKCanvas canvas, AvatarLayer layer)
        {
            SKBitmap? bitmap = null;
            
            // Get the appropriate image path
            string imagePath = layer.ImagePath;
            
            // For mouth layers, use current frame
            if (layer.IsMouthLayer && layer.MouthFramePaths.Count > 0)
            {
                if (layer.CurrentMouthFrame < layer.MouthFramePaths.Count)
                {
                    imagePath = layer.MouthFramePaths[layer.CurrentMouthFrame];
                }
            }
            
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                // Draw placeholder if no image
                DrawPlaceholder(canvas, layer);
                return;
            }
            
            bitmap = LoadBitmap(imagePath);
            
            if (bitmap == null)
            {
                DrawPlaceholder(canvas, layer);
                return;
            }
            
            // Calculate position based on bone attachment
            float x = 0, y = 0;
            
            if (!string.IsNullOrEmpty(layer.AttachedBoneId))
            {
                var bone = _preset?.Bones.FirstOrDefault(b => b.Id == layer.AttachedBoneId);
                if (bone != null)
                {
                    x = bone.X;
                    y = bone.Y;
                    
                    // Apply bone rotation
                    canvas.Save();
                    canvas.Translate(x, y);
                    canvas.RotateDegrees(bone.Rotation);
                    canvas.Translate(-x, -y);
                }
            }
            
            // Apply eye movement for eye layers
            if (layer.IsEyeLayer && !_animationController.IsBlinking)
            {
                x += _animationController.EyeOffsetX * layer.EyeMoveRadius;
                y += _animationController.EyeOffsetY * layer.EyeMoveRadius;
            }
            
            // Handle blinking for eye layers
            if (layer.IsEyeLayer && _animationController.IsBlinking)
            {
                // Scale Y to simulate blink
                canvas.Save();
                canvas.Translate(x + bitmap.Width / 2f, y + bitmap.Height / 2f);
                canvas.Scale(1f, 0.1f);
                canvas.Translate(-(x + bitmap.Width / 2f), -(y + bitmap.Height / 2f));
            }
            
            // Draw the bitmap
            var paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };
            
            if (layer.Opacity < 1f)
            {
                paint.Alpha = (byte)(layer.Opacity * 255);
            }
            
            var destRect = new SKRect(x, y, x + bitmap.Width, y + bitmap.Height);
            canvas.DrawBitmap(bitmap, destRect, paint);
            
            if (layer.IsEyeLayer && _animationController.IsBlinking)
            {
                canvas.Restore();
            }
            
            if (!string.IsNullOrEmpty(layer.AttachedBoneId))
            {
                canvas.Restore();
            }
        }
        
        private void DrawPlaceholder(SKCanvas canvas, AvatarLayer layer)
        {
            var paint = new SKPaint
            {
                Color = SKColors.Gray.WithAlpha((byte)(layer.Opacity * 128)),
                IsAntialias = true
            };
            
            var rect = new SKRect(50, 50, 150, 150);
            canvas.DrawRect(rect, paint);
            
            var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 14,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };
            
            canvas.DrawText(layer.Name, 100, 105, textPaint);
        }
        
        private SKBitmap? LoadBitmap(string path)
        {
            if (!_bitmapCache.ContainsKey(path))
            {
                try
                {
                    using var stream = File.OpenRead(path);
                    var bitmap = SKBitmap.Decode(stream);
                    _bitmapCache[path] = bitmap;
                }
                catch
                {
                    return null;
                }
            }
            
            return _bitmapCache[path];
        }
        
        private void ClearBitmapCache()
        {
            foreach (var bitmap in _bitmapCache.Values)
            {
                bitmap.Dispose();
            }
            _bitmapCache.Clear();
        }
        
        public void ReloadBitmaps()
        {
            ClearBitmapCache();
        }
    }
}
