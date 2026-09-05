using Avalonia.Controls;
using Avalonia.Input;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class SiparisDetayView : UserControl
{
    public SiparisDetayView()
    {
        InitializeComponent();
    }

    // Adding DoubleTapped handlers for dialog grids just in case, similar to FaturaDetayView
    private void OnStokDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.SiparisDetayViewModel vm)
        {
            vm.AddStokToGrid();
        }
    }

    private void OnCariDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.SiparisDetayViewModel vm)
        {
            vm.SelectCari();
        }
    }
}
