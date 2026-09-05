using Avalonia.Controls;
using Avalonia.Input;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class TeklifListView : UserControl
{
    public TeklifListView()
    {
        InitializeComponent();
    }
    
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.TeklifListViewModel vm && vm.SelectedTeklif != null)
        {
            vm.EditTeklifCommand.Execute(vm.SelectedTeklif);
        }
    }
}
