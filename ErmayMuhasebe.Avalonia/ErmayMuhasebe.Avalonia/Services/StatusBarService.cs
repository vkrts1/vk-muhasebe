using System;
using System.IO;
using System.Text.Json;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Services
{
    public class StatusBarSettings
    {
        public bool ShowVersion { get; set; } = true;
        public bool ShowUser { get; set; } = true;
        public bool ShowBranch { get; set; } = true;
        public bool ShowSystemStatus { get; set; } = true;
        public bool ShowCopyright { get; set; } = true;
        
        // New Features requested by user
        public bool ShowExchangeRates { get; set; } = true;
        public bool ShowDateTime { get; set; } = true;
        public bool ShowSystemUsage { get; set; } = false;
        public bool ShowSyncStatus { get; set; } = true;
        public bool ShowConnectionStatus { get; set; } = true;

        public List<string> ItemsOrder { get; set; } = new() 
        { 
            "Version", "User", "SyncStatus", "ConnectionStatus", "ExchangeRates", "SystemUsage", "DateTime", "Copyright" 
        };
        
        public Dictionary<string, double> ItemsMargins { get; set; } = new();
        
        public string CustomLeftText { get; set; } = "";
        public string CustomRightText { get; set; } = "";
        
        // IDE Layout Specifics
        public string IdeBranchName { get; set; } = "Main*";
        public string IdeStatusText { get; set; } = "Sistem Hazır";
        public string IdeEncoding { get; set; } = "UTF-8";
        public string IdeLanguage { get; set; } = "C#";
    }

    public class StatusBarService
    {
        private readonly string _configPath;
        public StatusBarSettings Settings { get; private set; }

        public event Action? SettingsChanged;

        public StatusBarService()
        {
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "statusbar_settings.json");
            Settings = LoadSettings();
        }

        private StatusBarSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<StatusBarSettings>(json) ?? new StatusBarSettings();
                }
            }
            catch { }
            return new StatusBarSettings();
        }

        public void SaveSettings(StatusBarSettings settings)
        {
            try
            {
                Settings = settings;
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                SettingsChanged?.Invoke();
            }
            catch { }
        }
    }
}
