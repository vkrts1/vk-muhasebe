using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class FaturaDetayView : UserControl
{
    public FaturaDetayView()
    {
        InitializeComponent();
    }

    private void OnCariDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.FaturaDetayViewModel vm)
        {
            vm.SelectCariCommand.Execute(null);
        }
    }
    private void OnStokDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.FaturaDetayViewModel vm)
        {
            // Execute AddStokToGridCommand which adds the selected item
            vm.AddStokToGridCommand.Execute(null);
        }
    }
}
