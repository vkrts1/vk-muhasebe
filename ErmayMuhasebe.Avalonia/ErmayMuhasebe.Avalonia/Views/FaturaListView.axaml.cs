using Avalonia.Controls;
using Avalonia.Input;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class FaturaListView : UserControl
{
    public FaturaListView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.FaturaListViewModel vm && vm.SelectedFatura != null)
        {
            vm.EditFaturaCommand.Execute(vm.SelectedFatura);
        }
    }
}
