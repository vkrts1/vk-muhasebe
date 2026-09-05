using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Services
{
    public class ThemeService
    {
        public enum AppTheme
        {
            ModernSaaS,     // Minimalist / Ferah (Light)
            IDEProfessional // VS Code Stili (Dark)
        }

        public AppTheme CurrentTheme { get; private set; } = AppTheme.ModernSaaS;
        private readonly string _configPath;

        public ThemeService()
        {
            _configPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "theme_settings.json");
            LoadSettings();
        }

        public event Action<AppTheme>? ThemeChanged;

        private void SaveSettings()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(new { Theme = CurrentTheme.ToString() });
                System.IO.File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                if (System.IO.File.Exists(_configPath))
                {
                    var json = System.IO.File.ReadAllText(_configPath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    if (data.TryGetProperty("Theme", out var themeProp) && Enum.TryParse<AppTheme>(themeProp.GetString(), out var theme))
                    {
                        CurrentTheme = theme;
                    }
                }
            }
            catch { }
        }

        public void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            SaveSettings();
            var app = Application.Current;
            if (app == null) return;

            // Define Theme Variables
            Color primary, background, surface, text;
            double controlCorner, cardCorner, layoutPadding;

            switch (theme)
            {
                case AppTheme.ModernSaaS:
                    // Web-Parity Colors (Pixel-Perfect Obsidian 100%)
                    primary = Color.Parse("#0061FF");           
                    background = Color.Parse("#050505");        
                    surface = Color.Parse("#050505");           
                    var drawerBg = Color.Parse("#030405");      
                    text = Color.Parse("#FFFFFF");              
                    
                    controlCorner = 8;
                    cardCorner = 16;
                    layoutPadding = 16;
                    
                    // Extra: Drawer special background handle via resource if needed
                    UpdateResource(app.Resources, "SidebarBackground", drawerBg);
                    UpdateResource(app.Resources, "SidebarBrush", new SolidColorBrush(drawerBg));
                    break;

                case AppTheme.IDEProfessional:
                    // VS Code Style (Pixel-Perfect Parity)
                    primary = Color.Parse("#007acc");      // VS Code Blue
                    background = Color.Parse("#1e1e1e");   // Editor Background
                    surface = Color.Parse("#252526");      // Side Bar/Panel
                    var ideDrawerBg = Color.Parse("#333333");
                    text = Color.Parse("#FFFFFF");         // White Text
                    
                    controlCorner = 0;
                    cardCorner = 4;
                    layoutPadding = 8;

                    UpdateResource(app.Resources, "SidebarBackground", ideDrawerBg);
                    UpdateResource(app.Resources, "SidebarBrush", new SolidColorBrush(ideDrawerBg));
                    break;

                default:
                    return;
            }

            // Update Resources
            var res = app.Resources;

            UpdateResource(res, "PrimaryColor", primary);
            UpdateResource(res, "PrimaryBrush", new SolidColorBrush(primary));
            
            UpdateResource(res, "BackgroundColor", background);
            UpdateResource(res, "BackgroundBrush", new SolidColorBrush(background));
            
            UpdateResource(res, "SurfaceColor", surface);
            UpdateResource(res, "SurfaceBrush", new SolidColorBrush(surface));
            
            UpdateResource(res, "TextColor", text);
            UpdateResource(res, "TextBrush", new SolidColorBrush(text));

            // Update Structure (as doubles)
            UpdateResource(res, "ControlCornerValue", controlCorner);
            UpdateResource(res, "CardCornerValue", cardCorner);
            UpdateResource(res, "LayoutPaddingValue", layoutPadding);
            
            // Notify subscribers
            ThemeChanged?.Invoke(theme);
            

        }

        private void UpdateResource(IResourceDictionary resources, object key, object value)
        {
            if (resources.ContainsKey(key))
            {
                resources[key] = value;
            }
            else
            {
                resources.Add(key, value);
            }
        }
    }
}
