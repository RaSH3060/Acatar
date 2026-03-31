using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StreamAvatar.Core
{
    /// <summary>
    /// Handles localization for RU/EN languages
    /// </summary>
    public class LocalizationManager
    {
        private Dictionary<string, string> _strings = new();
        private string _currentLanguage = "en";
        
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    LoadLanguage(_currentLanguage);
                }
            }
        }
        
        public event Action? OnLanguageChanged;
        
        public void Initialize(string language = "en")
        {
            _currentLanguage = language;
            LoadLanguage(_currentLanguage);
        }
        
        private void LoadLanguage(string languageCode)
        {
            _strings.Clear();
            
            var locPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Loc", $"{languageCode}.ini");
            
            if (!File.Exists(locPath))
            {
                // Fallback to English
                locPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Loc", "en.ini");
            }
            
            if (File.Exists(locPath))
            {
                var lines = File.ReadAllLines(locPath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                        continue;
                    
                    var parts = trimmed.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        _strings[key] = value;
                    }
                }
            }
            
            OnLanguageChanged?.Invoke();
        }
        
        public string Get(string key, params object[] args)
        {
            if (_strings.TryGetValue(key, out var value))
            {
                return args.Length > 0 ? string.Format(value, args) : value;
            }
            
            // Return key if translation not found
            return key;
        }
        
        public string this[string key] => Get(key);
    }

    /// <summary>
    /// Manages theme colors and styles
    /// </summary>
    public class ThemeManager
    {
        private Theme _currentTheme = new();
        
        public Theme CurrentTheme => _currentTheme;
        
        public event Action? OnThemeChanged;
        
        public void LoadTheme(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Theme file not found", path);
            
            var json = File.ReadAllText(path);
            _currentTheme = Newtonsoft.Json.JsonConvert.DeserializeObject<Theme>(json) ?? new Theme();
            
            OnThemeChanged?.Invoke();
        }
        
        public void SaveTheme(string path)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_currentTheme, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(path, json);
        }
        
        public void ApplyDefaultDarkTheme()
        {
            _currentTheme = new Theme
            {
                Name = "Dark Gamer",
                BackgroundPrimary = "#1E1E1E",
                BackgroundSecondary = "#2D2D30",
                AccentColor = "#0078D4",
                TextPrimary = "#FFFFFF",
                TextSecondary = "#B0B0B0",
                BorderColor = "#3F3F46"
            };
            
            OnThemeChanged?.Invoke();
        }
    }

    /// <summary>
    /// Theme color scheme definition
    /// </summary>
    public class Theme
    {
        public string Name { get; set; } = "Default";
        public string BackgroundPrimary { get; set; } = "#FFFFFF";
        public string BackgroundSecondary { get; set; } = "#F0F0F0";
        public string AccentColor { get; set; } = "#0078D4";
        public string TextPrimary { get; set; } = "#000000";
        public string TextSecondary { get; set; } = "#666666";
        public string BorderColor { get; set; } = "#CCCCCC";
        
        public System.Drawing.Color BackgroundPrimaryColor => System.Drawing.ColorTranslator.FromHtml(BackgroundPrimary);
        public System.Drawing.Color BackgroundSecondaryColor => System.Drawing.ColorTranslator.FromHtml(BackgroundSecondary);
        public System.Drawing.Color AccentColorValue => System.Drawing.ColorTranslator.FromHtml(AccentColor);
        public System.Drawing.Color TextPrimaryColor => System.Drawing.ColorTranslator.FromHtml(TextPrimary);
        public System.Drawing.Color TextSecondaryColor => System.Drawing.ColorTranslator.FromHtml(TextSecondary);
        public System.Drawing.Color BorderColorValue => System.Drawing.ColorTranslator.FromHtml(BorderColor);
    }
}
