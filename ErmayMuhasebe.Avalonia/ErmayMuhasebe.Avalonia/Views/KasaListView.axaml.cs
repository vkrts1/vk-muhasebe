using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class KasaListView : UserControl
{
    public KasaListView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.KasaListViewModel vm && vm.SelectedKasa != null)
        {
            vm.NavigateToDetay(vm.SelectedKasa);
        }
    }
}
