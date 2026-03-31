using System;
using System.Threading.Tasks;
using StreamAvatar.Core;

namespace StreamAvatar.Rendering
{
    /// <summary>
    /// Controls avatar animation including bones, eyes, mouth, and shake effects
    /// </summary>
    public class AnimationController
    {
        private AvatarPreset? _preset;
        private Random _random = new();
        private DateTime _lastBlinkTime;
        private DateTime _nextBlinkTime;
        private bool _isBlinking;
        private float _blinkStartTime;
        
        // Eye movement
        private float _eyeTargetX;
        private float _eyeTargetY;
        private float _eyeCurrentX;
        private float _eyeCurrentY;
        private DateTime _lastEyeMoveTime;
        
        // Shake effect
        private float _shakeOffsetX;
        private float _shakeOffsetY;
        private bool _isShaking;
        
        public event Action? OnAnimationUpdate;
        
        public AvatarPreset? Preset
        {
            get => _preset;
            set
            {
                _preset = value;
                ResetAnimationState();
            }
        }
        
        public float MouthAmplitude { get; set; }
        public float ShakeIntensity { get; set; }
        
        public void ResetAnimationState()
        {
            if (_preset == null) return;
            
            _lastBlinkTime = DateTime.Now;
            _nextBlinkTime = DateTime.Now + TimeSpan.FromSeconds(
                _random.NextSingle() * (_preset.AnimationSettings.BlinkIntervalMax - _preset.AnimationSettings.BlinkIntervalMin) 
                + _preset.AnimationSettings.BlinkIntervalMin);
            _isBlinking = false;
            
            _eyeTargetX = 0;
            _eyeTargetY = 0;
            _eyeCurrentX = 0;
            _eyeCurrentY = 0;
            _lastEyeMoveTime = DateTime.Now;
            
            _isShaking = false;
            _shakeOffsetX = 0;
            _shakeOffsetY = 0;
        }
        
        public void Update(float deltaTime)
        {
            if (_preset == null) return;
            
            var settings = _preset.AnimationSettings;
            
            // Update blink state
            UpdateBlink(deltaTime, settings);
            
            // Update eye movement
            UpdateEyeMovement(deltaTime, settings);
            
            // Update shake effect
            UpdateShake(deltaTime, settings);
            
            // Update mouth frames based on amplitude
            UpdateMouthFrames(settings);
            
            OnAnimationUpdate?.Invoke();
        }
        
        private void UpdateBlink(float deltaTime, AnimationSettings settings)
        {
            var now = DateTime.Now;
            
            if (!_isBlinking && now >= _nextBlinkTime)
            {
                _isBlinking = true;
                _blinkStartTime = (float)(now - _lastBlinkTime).TotalSeconds;
            }
            
            if (_isBlinking)
            {
                var blinkProgress = (float)(now - _lastBlinkTime).TotalSeconds - _blinkStartTime;
                
                if (blinkProgress >= settings.BlinkDuration)
                {
                    _isBlinking = false;
                    _lastBlinkTime = now;
                    _nextBlinkTime = now + TimeSpan.FromSeconds(
                        _random.NextSingle() * (settings.BlinkIntervalMax - settings.BlinkIntervalMin) 
                        + settings.BlinkIntervalMin);
                }
            }
        }
        
        private void UpdateEyeMovement(float deltaTime, AnimationSettings settings)
        {
            var now = DateTime.Now;
            
            // Move eyes to new target periodically
            if ((now - _lastEyeMoveTime).TotalSeconds > 2.0 / settings.IdleSpeed)
            {
                var angle = _random.NextSingle() * Math.PI * 2;
                var radius = _random.NextSingle() * 0.5f;
                
                _eyeTargetX = (float)Math.Cos(angle) * radius;
                _eyeTargetY = (float)Math.Sin(angle) * radius;
                _lastEyeMoveTime = now;
            }
            
            // Smoothly interpolate current position to target
            float lerpSpeed = 2f * deltaTime;
            _eyeCurrentX = Lerp(_eyeCurrentX, _eyeTargetX, lerpSpeed);
            _eyeCurrentY = Lerp(_eyeCurrentY, _eyeTargetY, lerpSpeed);
        }
        
        private void UpdateShake(float deltaTime, AnimationSettings settings)
        {
            if (MouthAmplitude >= settings.ShakeThreshold)
            {
                _isShaking = true;
                var intensity = settings.ShakeIntensity * (MouthAmplitude - settings.ShakeThreshold);
                
                _shakeOffsetX = (_random.NextSingle() - 0.5f) * 2 * intensity;
                _shakeOffsetY = (_random.NextSingle() - 0.5f) * 2 * intensity;
            }
            else
            {
                _isShaking = false;
                // Smoothly return to zero
                _shakeOffsetX = Lerp(_shakeOffsetX, 0, 5f * deltaTime);
                _shakeOffsetY = Lerp(_shakeOffsetY, 0, 5f * deltaTime);
            }
        }
        
        private void UpdateMouthFrames(AnimationSettings settings)
        {
            if (_preset == null) return;
            
            foreach (var layer in _preset.Layers)
            {
                if (layer.IsMouthLayer && layer.MouthFramePaths.Count > 0)
                {
                    // Map amplitude to frame index
                    int frameCount = Math.Min(layer.MouthFramePaths.Count, settings.MouthFrameCount);
                    int frameIndex = (int)(MouthAmplitude * settings.MouthSensitivity * frameCount);
                    frameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);
                    
                    layer.CurrentMouthFrame = frameIndex;
                }
            }
        }
        
        private static float Lerp(float a, float b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return a + (b - a) * t;
        }
        
        public bool IsBlinking => _isBlinking;
        public float EyeOffsetX => _eyeCurrentX;
        public float EyeOffsetY => _eyeCurrentY;
        public float ShakeOffsetX => _shakeOffsetX;
        public float ShakeOffsetY => _shakeOffsetY;
    }
}
