using Avalonia.Controls;
using Avalonia.Input;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class TeklifDetayView : UserControl
{
    public TeklifDetayView()
    {
        InitializeComponent();
    }

    private void OnStokDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.TeklifDetayViewModel vm)
        {
            vm.AddStokToGrid();
        }
    }

    private void OnCariDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.TeklifDetayViewModel vm)
        {
            vm.SelectCari();
        }
    }
}
