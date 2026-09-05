using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class NotificationItem : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Icon { get; set; } = "Alert";
    
    [JsonIgnore]
    public Type? TargetPageType { get; set; }

    public string? TargetPageTypeName 
    { 
        get => TargetPageType?.AssemblyQualifiedName; 
        set 
        {
            if (!string.IsNullOrEmpty(value))
            {
                try { TargetPageType = Type.GetType(value); } catch { }
            }
        }
    }

    [ObservableProperty]
    private bool _isRead = false;

    public NotificationItem() { }

    public NotificationItem(string title, string message, string icon, Type? targetPageType = null)
    {
        Title = title;
        Message = message;
        Icon = icon;
        TargetPageType = targetPageType;
    }
}
