using System;
using NAudio.Wave;
using NAudio.Dsp;

namespace StreamAvatar.Audio
{
    /// <summary>
    /// Handles audio capture and analysis from microphone
    /// </summary>
    public class AudioEngine : IDisposable
    {
        private WaveInEvent? _waveIn;
        private BufferedWaveProvider? _bufferedWaveProvider;
        private bool _disposed;
        
        public float CurrentAmplitude { get; private set; }
        public float PeakAmplitude { get; private set; }
        
        public event Action<float>? OnAmplitudeChanged;
        public event Action? OnAudioDataReady;
        
        public bool IsRecording { get; private set; }
        
        public void StartCapture(string? deviceId = null)
        {
            StopCapture();
            
            if (string.IsNullOrEmpty(deviceId))
            {
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(44100, 16, 1),
                    BufferMilliseconds = 50
                };
            }
            else
            {
                // Find device by ID
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var caps = WaveIn.GetCapabilities(i);
                    if (caps.ProductGuid.ToString() == deviceId || caps.ProductName.Contains(deviceId))
                    {
                        _waveIn = new WaveInEvent
                        {
                            DeviceNumber = i,
                            WaveFormat = new WaveFormat(44100, 16, 1),
                            BufferMilliseconds = 50
                        };
                        break;
                    }
                }
                
                if (_waveIn == null)
                {
                    _waveIn = new WaveInEvent
                    {
                        WaveFormat = new WaveFormat(44100, 16, 1)
                    };
                }
            }
            
            _bufferedWaveProvider = new BufferedWaveProvider(_waveIn.WaveFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(500),
                DiscardOnBufferOverflow = true
            };
            
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            
            _waveIn.StartRecording();
            IsRecording = true;
        }
        
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_bufferedWaveProvider == null) return;
            
            _bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            
            // Calculate amplitude
            float maxSample = 0;
            for (int i = 0; i < e.BytesRecorded / 2; i++)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i * 2);
                float normalized = sample / 32768f;
                if (Math.Abs(normalized) > maxSample)
                {
                    maxSample = Math.Abs(normalized);
                }
            }
            
            CurrentAmplitude = maxSample;
            
            // Smooth peak decay
            if (CurrentAmplitude > PeakAmplitude)
            {
                PeakAmplitude = CurrentAmplitude;
            }
            else
            {
                PeakAmplitude *= 0.95f;
            }
            
            OnAmplitudeChanged?.Invoke(CurrentAmplitude);
            OnAudioDataReady?.Invoke();
        }
        
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            IsRecording = false;
        }
        
        public void StopCapture()
        {
            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
                _waveIn = null;
            }
            
            IsRecording = false;
        }
        
        /// <summary>
        /// Get available input devices
        /// </summary>
        public static string[] GetInputDevices()
        {
            var devices = new string[WaveIn.DeviceCount];
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                devices[i] = WaveIn.GetCapabilities(i).ProductName;
            }
            return devices;
        }
        
        /// <summary>
        /// Get device ID by name
        /// </summary>
        public static string GetDeviceIdByName(string name)
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                if (caps.ProductName == name)
                {
                    return caps.ProductGuid.ToString();
                }
            }
            return "";
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopCapture();
                    _bufferedWaveProvider?.ClearBuffer();
                }
                _disposed = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Audio effects processor (Reverb, Pitch Shift)
    /// </summary>
    public class AudioProcessor
    {
        private float _reverbMix = 0.3f;
        private float _pitchShift = 0f;
        
        public float ReverbMix
        {
            get => _reverbMix;
            set => _reverbMix = Math.Clamp(value, 0f, 1f);
        }
        
        public float PitchShiftAmount
        {
            get => _pitchShift;
            set => _pitchShift = Math.Clamp(value, -12f, 12f);
        }
        
        public bool ReverbEnabled { get; set; }
        public bool PitchShiftEnabled { get; set; }
        
        // Simple reverb implementation using delay lines
        private readonly float[] _delayLine = new float[44100];
        private int _delayIndex;
        
        public float[] Process(float[] samples, int sampleRate = 44100)
        {
            if (!ReverbEnabled && !PitchShiftEnabled)
                return samples;
            
            var output = new float[samples.Length];
            
            for (int i = 0; i < samples.Length; i++)
            {
                float dry = samples[i];
                float wet = 0f;
                
                // Apply reverb
                if (ReverbEnabled)
                {
                    wet = _delayLine[_delayIndex];
                    _delayLine[_delayIndex] = dry * 0.5f + wet * 0.5f;
                    _delayIndex = (_delayIndex + 1) % _delayLine.Length;
                }
                
                // Apply pitch shift (simplified - just mix for now)
                float processed = dry;
                if (PitchShiftEnabled && Math.Abs(_pitchShift) > 0.01f)
                {
                    // Basic pitch shift approximation
                    int offset = (int)(_pitchShift * 100);
                    int readIndex = i + offset;
                    if (readIndex >= 0 && readIndex < samples.Length)
                    {
                        processed = samples[readIndex];
                    }
                }
                
                // Mix wet/dry
                if (ReverbEnabled)
                {
                    processed = processed * (1 - _reverbMix) + wet * _reverbMix;
                }
                
                output[i] = processed;
            }
            
            return output;
        }
    }
}
