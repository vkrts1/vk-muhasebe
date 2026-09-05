using Avalonia.Controls;
using Avalonia.Input;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class BankaListView : UserControl
{
    public BankaListView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.BankaListViewModel vm && vm.SelectedBanka != null)
        {
            vm.NavigateToDetay(vm.SelectedBanka);
        }
    }
}
