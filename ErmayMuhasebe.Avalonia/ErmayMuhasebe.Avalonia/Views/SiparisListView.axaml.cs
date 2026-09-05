using Avalonia.Controls;
using Avalonia.Input;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class SiparisListView : UserControl
{
    public SiparisListView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.SiparisListViewModel vm && vm.SelectedSiparis != null)
        {
            vm.EditSiparisCommand.Execute(vm.SelectedSiparis);
        }
    }
}
