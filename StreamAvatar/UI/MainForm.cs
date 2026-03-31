using System;
using System.Drawing;
using System.Windows.Forms;
using StreamAvatar.Core;
using StreamAvatar.Audio;
using StreamAvatar.Rendering;
using StreamAvatar.WebServer;
using SkiaSharp.Views.WindowsForms;

namespace StreamAvatar.UI
{
    /// <summary>
    /// Main application form with modern gamer-style dark theme
    /// </summary>
    public class MainForm : Form
    {
        // Core components
        private AvatarRenderer _renderer;
        private AudioEngine _audioEngine;
        private ObsWebServer _webServer;
        private AnimationController _animationController;
        
        // Managers
        private LocalizationManager _localization;
        private ThemeManager _themeManager;
        
        // UI Controls
        private GLControl _previewCanvas;
        private Label lblStatus;
        private Button btnStartAudio;
        private Button btnStopAudio;
        private Button btnExportPreset;
        private Button btnImportPreset;
        private Button btnOpenUrl;
        private ComboBox cmbLanguage;
        private ComboBox cmbTheme;
        private ComboBox cmbAudioDevice;
        private TrackBar trkVolume;
        private TrackBar trkMouthSensitivity;
        private TrackBar trkShakeThreshold;
        private CheckBox chkReverb;
        private CheckBox chkPitchShift;
        private CheckBox chkVirtualMic;
        private Label lblUrl;
        private Timer _renderTimer;
        private Panel _controlPanel;
        private GroupBox _grpAudio;
        private GroupBox _grpAnimation;
        private GroupBox _grpEffects;
        private GroupBox _grpSettings;
        
        private string _serverUrl = "";
        private bool _isAudioRunning;

        public MainForm()
        {
            // Initialize core components
            _renderer = new AvatarRenderer();
            _audioEngine = new AudioEngine();
            _animationController = _renderer.AnimationController;
            _webServer = new ObsWebServer(_renderer);
            _localization = new LocalizationManager();
            _themeManager = new ThemeManager();
            
            InitializeComponent();
            
            // Apply default theme
            _themeManager.ApplyDefaultDarkTheme();
            ApplyTheme();
            
            // Initialize localization
            _localization.Initialize("en");
            UpdateLocalization();
            
            // Setup render timer
            _renderTimer = new Timer();
            _renderTimer.Interval = 16; // ~60 FPS
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();
            
            // Hook up events
            _audioEngine.OnAmplitudeChanged += OnAmplitudeChanged;
            _webServer.OnUrlGenerated += OnUrlGenerated;
            _localization.OnLanguageChanged += UpdateLocalization;
            _themeManager.OnThemeChanged += ApplyTheme;
            
            // Load default preset
            LoadDefaultPreset();
        }
        
        private void InitializeComponent()
        {
            this.Text = "StreamAvatar - Animated Avatar for Streaming";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            
            // Preview canvas (SkiaSharp GL control)
            _previewCanvas = new GLControl();
            _previewCanvas.Dock = DockStyle.Fill;
            _previewCanvas.PaintSurface += OnPaintSurface;
            
            // Control panel on the right
            _controlPanel = new Panel();
            _controlPanel.Dock = DockStyle.Right;
            _controlPanel.Width = 320;
            _controlPanel.Padding = new Padding(10);
            
            // Status label at bottom
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 30;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            
            // Audio controls group
            _grpAudio = new GroupBox();
            _grpAudio.Text = "Audio Input";
            _grpAudio.Location = new Point(10, 10);
            _grpAudio.Size = new Size(300, 140);
            
            cmbAudioDevice = new ComboBox();
            cmbAudioDevice.Location = new Point(10, 25);
            cmbAudioDevice.Size = new Size(280, 23);
            cmbAudioDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAudioDevice.Items.AddRange(AudioEngine.GetInputDevices());
            if (cmbAudioDevice.Items.Count > 0)
                cmbAudioDevice.SelectedIndex = 0;
            
            btnStartAudio = new Button();
            btnStartAudio.Text = "Start Microphone";
            btnStartAudio.Location = new Point(10, 55);
            btnStartAudio.Size = new Size(135, 35);
            btnStartAudio.BackColor = Color.FromArgb(0, 120, 215);
            btnStartAudio.ForeColor = Color.White;
            btnStartAudio.FlatStyle = FlatStyle.Flat;
            btnStartAudio.Click += BtnStartAudio_Click;
            
            btnStopAudio = new Button();
            btnStopAudio.Text = "Stop";
            btnStopAudio.Location = new Point(155, 55);
            btnStopAudio.Size = new Size(135, 35);
            btnStopAudio.BackColor = Color.FromArgb(200, 50, 50);
            btnStopAudio.ForeColor = Color.White;
            btnStopAudio.FlatStyle = FlatStyle.Flat;
            btnStopAudio.Click += BtnStopAudio_Click;
            btnStopAudio.Enabled = false;
            
            trkVolume = new TrackBar();
            trkVolume.Location = new Point(10, 95);
            trkVolume.Size = new Size(280, 45);
            trkVolume.Minimum = 0;
            trkVolume.Maximum = 100;
            trkVolume.Value = 100;
            trkVolume.TickFrequency = 10;
            var lblVol = new Label();
            lblVol.Text = "Volume:";
            lblVol.Location = new Point(10, 75);
            lblVol.AutoSize = true;
            
            _grpAudio.Controls.Add(cmbAudioDevice);
            _grpAudio.Controls.Add(btnStartAudio);
            _grpAudio.Controls.Add(btnStopAudio);
            _grpAudio.Controls.Add(trkVolume);
            _grpAudio.Controls.Add(lblVol);
            
            // Animation controls group
            _grpAnimation = new GroupBox();
            _grpAnimation.Text = "Animation";
            _grpAnimation.Location = new Point(10, 160);
            _grpAnimation.Size = new Size(300, 120);
            
            trkMouthSensitivity = new TrackBar();
            trkMouthSensitivity.Location = new Point(10, 25);
            trkMouthSensitivity.Size = new Size(280, 45);
            trkMouthSensitivity.Minimum = 1;
            trkMouthSensitivity.Maximum = 200;
            trkMouthSensitivity.Value = 100;
            var lblMouth = new Label();
            lblMouth.Text = "Mouth Sensitivity:";
            lblMouth.Location = new Point(10, 5);
            lblMouth.AutoSize = true;
            
            trkShakeThreshold = new TrackBar();
            trkShakeThreshold.Location = new Point(10, 70);
            trkShakeThreshold.Size = new Size(280, 45);
            trkShakeThreshold.Minimum = 10;
            trkShakeThreshold.Maximum = 100;
            trkShakeThreshold.Value = 80;
            var lblShake = new Label();
            lblShake.Text = "Shake Threshold:";
            lblShake.Location = new Point(10, 50);
            lblShake.AutoSize = true;
            
            _grpAnimation.Controls.Add(trkMouthSensitivity);
            _grpAnimation.Controls.Add(lblMouth);
            _grpAnimation.Controls.Add(trkShakeThreshold);
            _grpAnimation.Controls.Add(lblShake);
            
            // Effects group
            _grpEffects = new GroupBox();
            _grpEffects.Text = "Audio Effects";
            _grpEffects.Location = new Point(10, 290);
            _grpEffects.Size = new Size(300, 120);
            
            chkReverb = new CheckBox();
            chkReverb.Text = "Reverb";
            chkReverb.Location = new Point(10, 25);
            chkReverb.AutoSize = true;
            
            chkPitchShift = new CheckBox();
            chkPitchShift.Text = "Pitch Shift";
            chkPitchShift.Location = new Point(10, 55);
            chkPitchShift.AutoSize = true;
            
            chkVirtualMic = new CheckBox();
            chkVirtualMic.Text = "Virtual Microphone Output";
            chkVirtualMic.Location = new Point(10, 85);
            chkVirtualMic.AutoSize = true;
            
            _grpEffects.Controls.Add(chkReverb);
            _grpEffects.Controls.Add(chkPitchShift);
            _grpEffects.Controls.Add(chkVirtualMic);
            
            // Settings group
            _grpSettings = new GroupBox();
            _grpSettings.Text = "Settings";
            _grpSettings.Location = new Point(10, 420);
            _grpSettings.Size = new Size(300, 180);
            
            cmbLanguage = new ComboBox();
            cmbLanguage.Location = new Point(10, 25);
            cmbLanguage.Size = new Size(135, 23);
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Items.AddRange(new[] { "English", "Русский" });
            cmbLanguage.SelectedIndex = 0;
            cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
            
            cmbTheme = new ComboBox();
            cmbTheme.Location = new Point(155, 25);
            cmbTheme.Size = new Size(135, 23);
            cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTheme.Items.AddRange(new[] { "Dark Gamer", "Light", "Cyberpunk" });
            cmbTheme.SelectedIndex = 0;
            cmbTheme.SelectedIndexChanged += CmbTheme_SelectedIndexChanged;
            
            btnExportPreset = new Button();
            btnExportPreset.Text = "Export Preset";
            btnExportPreset.Location = new Point(10, 60);
            btnExportPreset.Size = new Size(135, 35);
            btnExportPreset.BackColor = Color.FromArgb(0, 120, 215);
            btnExportPreset.ForeColor = Color.White;
            btnExportPreset.FlatStyle = FlatStyle.Flat;
            btnExportPreset.Click += BtnExportPreset_Click;
            
            btnImportPreset = new Button();
            btnImportPreset.Text = "Import Preset";
            btnImportPreset.Location = new Point(155, 60);
            btnImportPreset.Size = new Size(135, 35);
            btnImportPreset.BackColor = Color.FromArgb(0, 120, 215);
            btnImportPreset.ForeColor = Color.White;
            btnImportPreset.FlatStyle = FlatStyle.Flat;
            btnImportPreset.Click += BtnImportPreset_Click;
            
            lblUrl = new Label();
            lblUrl.Text = "OBS URL: Not started";
            lblUrl.Location = new Point(10, 105);
            lblUrl.Size = new Size(280, 40);
            lblUrl.TextAlign = ContentAlignment.TopLeft;
            lblUrl.AutoEllipsis = true;
            
            btnOpenUrl = new Button();
            btnOpenUrl.Text = "Open in Browser";
            btnOpenUrl.Location = new Point(10, 145);
            btnOpenUrl.Size = new Size(280, 30);
            btnOpenUrl.BackColor = Color.FromArgb(0, 200, 100);
            btnOpenUrl.ForeColor = Color.White;
            btnOpenUrl.FlatStyle = FlatStyle.Flat;
            btnOpenUrl.Click += BtnOpenUrl_Click;
            
            _grpSettings.Controls.Add(cmbLanguage);
            _grpSettings.Controls.Add(cmbTheme);
            _grpSettings.Controls.Add(btnExportPreset);
            _grpSettings.Controls.Add(btnImportPreset);
            _grpSettings.Controls.Add(lblUrl);
            _grpSettings.Controls.Add(btnOpenUrl);
            
            // Add groups to control panel
            _controlPanel.Controls.Add(_grpAudio);
            _controlPanel.Controls.Add(_grpAnimation);
            _controlPanel.Controls.Add(_grpEffects);
            _controlPanel.Controls.Add(_grpSettings);
            
            // Add controls to form
            this.Controls.Add(_previewCanvas);
            this.Controls.Add(_controlPanel);
            this.Controls.Add(lblStatus);
        }
        
        private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
        {
            _renderer.CanvasWidth = e.BackendRenderTarget.Width;
            _renderer.CanvasHeight = e.BackendRenderTarget.Height;
            _renderer.Render(e.Surface.Canvas);
        }
        
        private void OnRenderTick(object? sender, EventArgs e)
        {
            // Update animation based on audio amplitude
            float deltaTime = 0.016f; // ~60fps
            _animationController.Update(deltaTime);
            
            // Refresh preview
            _previewCanvas.Invalidate();
            
            // Update status
            if (_isAudioRunning)
            {
                lblStatus.Text = $"Audio Level: {_audioEngine.CurrentAmplitude:P0} | FPS: 60";
            }
            else
            {
                lblStatus.Text = "Ready - Start microphone to begin";
            }
        }
        
        private void OnAmplitudeChanged(float amplitude)
        {
            _animationController.MouthAmplitude = amplitude;
            
            // Check shake threshold
            var settings = _renderer.Preset?.AnimationSettings ?? new AnimationSettings();
            if (amplitude >= settings.ShakeThreshold)
            {
                _animationController.ShakeIntensity = settings.ShakeIntensity;
            }
        }
        
        private void OnUrlGenerated(string url)
        {
            _serverUrl = url;
            lblUrl.Text = $"OBS URL: {url}/avatar";
            lblStatus.Text = $"Web server started at {url}";
        }
        
        private void BtnStartAudio_Click(object? sender, EventArgs e)
        {
            try
            {
                string deviceId = "";
                if (cmbAudioDevice.SelectedItem != null)
                {
                    deviceId = AudioEngine.GetDeviceIdByName(cmbAudioDevice.SelectedItem.ToString()!);
                }
                
                _audioEngine.StartCapture(deviceId);
                _isAudioRunning = true;
                
                btnStartAudio.Enabled = false;
                btnStopAudio.Enabled = true;
                
                // Start web server if not already running
                if (!_webServer.IsRunning)
                {
                    _ = _webServer.StartAsync();
                }
                
                lblStatus.Text = "Microphone started";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start microphone: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BtnStopAudio_Click(object? sender, EventArgs e)
        {
            _audioEngine.StopCapture();
            _isAudioRunning = false;
            
            btnStartAudio.Enabled = true;
            btnStopAudio.Enabled = false;
            
            lblStatus.Text = "Microphone stopped";
        }
        
        private void BtnExportPreset_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog();
            dialog.Filter = "Avatar Preset (*.avatar)|*.avatar|All Files (*.*)|*.*";
            dialog.Title = "Export Avatar Preset";
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _renderer.Preset?.SaveToFile(dialog.FileName);
                    MessageBox.Show("Preset exported successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void BtnImportPreset_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "Avatar Preset (*.avatar)|*.avatar|All Files (*.*)|*.*";
            dialog.Title = "Import Avatar Preset";
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var preset = AvatarPreset.LoadFromFile(dialog.FileName);
                    _renderer.Preset = preset;
                    MessageBox.Show("Preset imported successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void BtnOpenUrl_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_serverUrl))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"{_serverUrl}/avatar",
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("Please start the microphone first to generate the URL.", 
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void CmbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _localization.CurrentLanguage = cmbLanguage.SelectedIndex == 0 ? "en" : "ru";
        }
        
        private void CmbTheme_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Apply different themes based on selection
            switch (cmbTheme.SelectedIndex)
            {
                case 0: // Dark Gamer
                    _themeManager.ApplyDefaultDarkTheme();
                    break;
                case 1: // Light
                    // Implement light theme
                    break;
                case 2: // Cyberpunk
                    // Implement cyberpunk theme
                    break;
            }
            ApplyTheme();
        }
        
        private void ApplyTheme()
        {
            var theme = _themeManager.CurrentTheme;
            
            this.BackColor = theme.BackgroundPrimaryColor;
            this.ForeColor = theme.TextPrimaryColor;
            
            _controlPanel.BackColor = theme.BackgroundSecondaryColor;
            _controlPanel.ForeColor = theme.TextPrimaryColor;
            
            foreach (Control ctrl in _controlPanel.Controls)
            {
                if (ctrl is GroupBox grp)
                {
                    grp.BackColor = theme.BackgroundSecondaryColor;
                    grp.ForeColor = theme.TextPrimaryColor;
                    
                    foreach (Control inner in grp.Controls)
                    {
                        inner.BackColor = theme.BackgroundSecondaryColor;
                        inner.ForeColor = theme.TextPrimaryColor;
                    }
                }
            }
            
            lblStatus.BackColor = theme.BackgroundPrimaryColor;
            lblStatus.ForeColor = theme.TextPrimaryColor;
        }
        
        private void UpdateLocalization()
        {
            this.Text = _localization["app_title"];
            _grpAudio.Text = _localization["grp_audio"];
            _grpAnimation.Text = _localization["grp_animation"];
            _grpEffects.Text = _localization["grp_effects"];
            _grpSettings.Text = _localization["grp_settings"];
            btnStartAudio.Text = _localization["btn_start_mic"];
            btnStopAudio.Text = _localization["btn_stop"];
            btnExportPreset.Text = _localization["btn_export"];
            btnImportPreset.Text = _localization["btn_import"];
        }
        
        private void LoadDefaultPreset()
        {
            var preset = new AvatarPreset
            {
                Name = "Default Avatar",
                Description = "A simple starter avatar"
            };
            
            // Add placeholder layers
            preset.Layers.Add(new AvatarLayer
            {
                Name = "Body",
                Order = 0,
                Visible = true
            });
            
            preset.Layers.Add(new AvatarLayer
            {
                Name = "Eyes",
                Order = 1,
                IsEyeLayer = true,
                EyeMoveRadius = 15f,
                Visible = true
            });
            
            preset.Layers.Add(new AvatarLayer
            {
                Name = "Mouth",
                Order = 2,
                IsMouthLayer = true,
                MouthFrameCount = 5,
                Visible = true
            });
            
            _renderer.Preset = preset;
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _renderTimer.Stop();
            _renderTimer.Dispose();
            _audioEngine.Dispose();
            _webServer.Dispose();
            base.OnFormClosing(e);
        }
    }
}
