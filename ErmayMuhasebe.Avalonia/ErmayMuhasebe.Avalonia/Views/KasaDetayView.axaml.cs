using Avalonia.Controls;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class KasaDetayView : UserControl
{
    public KasaDetayView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.KasaDetayViewModel vm && vm.SelectedHareket != null)
        {
            vm.ViewTransactionDetailCommand.Execute(vm.SelectedHareket);
        }
    }
}
