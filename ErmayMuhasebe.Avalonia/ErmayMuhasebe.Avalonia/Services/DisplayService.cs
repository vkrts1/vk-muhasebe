using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.IO;
using System.Text.Json;

namespace ErmayMuhasebe.Services
{
    public class DisplaySettings
    {
        public double Scaling { get; set; } = 1.0;
        public bool IsAutoScalingEnabled { get; set; } = true;
    }

    public class DisplayService
    {
        private readonly string _configPath;
        private double _scaling = 1.0;
        private bool _isAutoScalingEnabled = true;

        public DisplayService()
        {
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "display_settings.json");
            LoadSettings();
        }

        private double _fitnessScale = 2.0;
        public double FitnessScale 
        { 
            get => _fitnessScale; 
            private set => _fitnessScale = value; 
        }

        public double Scaling
        {
            get => _scaling;
            private set
            {
                // ELASIC CONSTRAINT: Scale can NEVER exceed what fits in the current window
                // Clamp between 0.5 and the current FitnessScale (max 2.0)
                var upperLimit = Math.Min(2.0, FitnessScale);
                var clampedValue = Math.Clamp(value, 0.5, upperLimit);
                
                if (Math.Abs(_scaling - clampedValue) > 0.001)
                {
                    _scaling = clampedValue;
                    SaveSettings();
                    ScalingChanged?.Invoke(_scaling);
                }
            }
        }

        public bool IsAutoScalingEnabled
        {
            get => _isAutoScalingEnabled;
            set
            {
                if (_isAutoScalingEnabled != value)
                {
                    _isAutoScalingEnabled = value;
                    SaveSettings();
                    if (_isAutoScalingEnabled)
                    {
                        AutoDetectScaling(true);
                    }
                    AutoScalingChanged?.Invoke(_isAutoScalingEnabled);
                }
            }
        }

        public event Action<double>? ScalingChanged;
        public event Action<bool>? AutoScalingChanged;

        public void SetScaling(double scaling)
        {
            // Manual scaling disables auto scaling - user explicitly chose a scale
            if (IsAutoScalingEnabled)
            {
                IsAutoScalingEnabled = false;
            }
            Scaling = scaling;
        }

        private void SaveSettings()
        {
            try 
            { 
                var settings = new DisplaySettings 
                { 
                    Scaling = _scaling, 
                    IsAutoScalingEnabled = _isAutoScalingEnabled 
                };
                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var settings = JsonSerializer.Deserialize<DisplaySettings>(json);
                    if (settings != null)
                    {
                        _isAutoScalingEnabled = settings.IsAutoScalingEnabled;
                        _scaling = Math.Clamp(settings.Scaling, 0.5, 2.0);
                    }
                }
                else
                {
                    // If no settings exist, default to auto-detect on first run
                    _isAutoScalingEnabled = true;
                }
            }
            catch { }
        }

        public void AutoDetectScaling(bool force = false)
        {
            if (!force && !IsAutoScalingEnabled) return;

            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var window = desktop.MainWindow;
                    if (window == null) return;

                    var screen = window.Screens.ScreenFromVisual(window) ?? window.Screens.Primary;
                    if (screen == null) return;

                    // Base monitor properties
                    double screenWidth = screen.WorkingArea.Width;
                    double screenHeight = screen.WorkingArea.Height;
                    
                    // Actual window properties (fallback to screen if minimized or not yet shown)
                    double targetWidth = (window.ClientSize.Width > 100) ? window.ClientSize.Width : screenWidth;
                    double targetHeight = (window.ClientSize.Height > 100) ? window.ClientSize.Height : screenHeight;

                    // Reference Design Size: 1600x900 is a good "sweet spot" for 100% scale
                    // If the available area is less than this, we should scale down to fit.
                    
                    double calculatedScale = 1.0;

                    // Step 1: Monitor-based baseline
                    if (screenHeight <= 768) calculatedScale = 0.80;
                    else if (screenHeight <= 900) calculatedScale = 1.0;
                    else if (screenHeight <= 1080) calculatedScale = 1.10;
                    else if (screenHeight <= 1440) calculatedScale = 1.25;
                    else calculatedScale = 1.50;

                    // Step 2: Window-based "Fit to Screen" (Prevention of scrollbars)
                    // We assume the app needs at least 1200px width and 700px height logically to look "good" without scrollbars
                    double minLogicalWidth = 1280;
                    double minLogicalHeight = 720;

                    double widthScaleLimit = targetWidth / minLogicalWidth;
                    double heightScaleLimit = targetHeight / minLogicalHeight;

                    // The actual limit to prevent scrollbars is the minimum of these two
                    FitnessScale = Math.Min(widthScaleLimit, heightScaleLimit);
                    FitnessScale = Math.Clamp(FitnessScale, 0.5, 2.0);

                    // We choose the lower of the fitness scale and our baseline (if auto scaling)
                    calculatedScale = Math.Min(calculatedScale, FitnessScale);
                    calculatedScale = Math.Clamp(calculatedScale, 0.60, 2.0);
                    calculatedScale = Math.Clamp(calculatedScale, 0.60, 2.0);
                    
                    // Set scaling
                    Scaling = Math.Round(calculatedScale, 2);
                }
            }
            catch
            {
                if (force) Scaling = 1.0;
            }
        }
    }
}

