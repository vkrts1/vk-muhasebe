using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Shared.ViewModels;

/// <summary>
/// Base class for all ViewModels in the application.
/// Provides common functionality for both Avalonia and Blazor implementations.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    public event Action? RequestClose;

    protected virtual void OnRequestClose() => RequestClose?.Invoke();

    public bool IsMobile => System.OperatingSystem.IsAndroid() || System.OperatingSystem.IsIOS() || System.OperatingSystem.IsBrowser();

    /// <summary>
    /// Called when the view is navigated to.
    /// Override this to load data or perform initialization.
    /// </summary>
    public virtual void OnNavigatedTo() { }

    /// <summary>
    /// Called when the view is navigated away from.
    /// Override this to cleanup resources or save state.
    /// </summary>
    public virtual void OnNavigatedFrom() { }

    /// <summary>
    /// Indicates if the ViewModel is currently loading data.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Error message to display to the user.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Success message to display to the user.
    /// </summary>
    [ObservableProperty]
    private string? _successMessage;

    /// <summary>
    /// Warning message to display to the user.
    /// </summary>
    [ObservableProperty]
    private string? _warningMessage;

    /// <summary>
    /// Platform-specific UI thread invocation.
    /// Default implementation runs synchronously.
    /// </summary>
    protected virtual Task InvokeOnUIThreadAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    // GENERAL CONFIRMATION DIALOG LOGIC
    [ObservableProperty] private bool _isConfirmVisible;
    [ObservableProperty] private string _confirmTitle = "Onay";
    [ObservableProperty] private string _confirmMessage = "Bu işlemi yapmak istediğinize emin misiniz?";
    private Func<Task>? _onConfirmAction;

    [RelayCommand]
    public void ConfirmNo()
    {
        IsConfirmVisible = false;
        _onConfirmAction = null;
    }

    [RelayCommand]
    public async Task ConfirmYesAsync()
    {
        var action = _onConfirmAction;
        IsConfirmVisible = false;
        _onConfirmAction = null;
        
        if (action != null)
        {
            try
            {
                await action.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"İşlem sırasında bir hata oluştu: {ex.Message}";
            }
        }
    }

    protected void ShowConfirm(string title, string message, Func<Task> onConfirm)
    {
        ConfirmTitle = title;
        ConfirmMessage = message;
        _onConfirmAction = onConfirm;
        IsConfirmVisible = true;
    }

    /// <summary>
    /// Handles the Escape key press.
    /// Can be overridden in derived ViewModels to close specific dialogs.
    /// </summary>
    [RelayCommand]
    public virtual void OnEscape()
    {
        if (IsConfirmVisible) 
        {
            ConfirmNo();
            return;
        }
        
        ErrorMessage = null;
        SuccessMessage = null;
        WarningMessage = null;
    }
}
