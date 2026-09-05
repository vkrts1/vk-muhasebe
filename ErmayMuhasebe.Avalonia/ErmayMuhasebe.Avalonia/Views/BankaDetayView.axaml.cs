using Avalonia.Controls;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class BankaDetayView : UserControl
{
    public BankaDetayView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.BankaDetayViewModel vm && vm.SelectedHareket != null)
        {
            if (vm.ViewTransactionDetailCommand.CanExecute(vm.SelectedHareket))
                vm.ViewTransactionDetailCommand.Execute(vm.SelectedHareket);
        }
    }

    private void OnCariDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.BankaDetayViewModel vm && vm.SelectedCariForSelection != null)
        {
            vm.ConfirmCariSelectionCommand.Execute(null);
        }
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ViewModels.BankaDetayViewModel vm)
        {
            vm.RequestFilePick = OpenFilePickerAsync;
        }
    }

    private async System.Threading.Tasks.Task<string?> OpenFilePickerAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Dekont Seç",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            return files[0].Path.LocalPath;
        }
        return null;
    }
}
