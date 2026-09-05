using Avalonia.Controls;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class CariSelectionDialog : UserControl
{
    public CariSelectionDialog()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is ErmayMuhasebe.Models.CariKart cari)
        {
            if (DataContext is ViewModels.CariListViewModel vm)
            {
                vm.SelectCariForTransactionCommand.Execute(cari);
            }
        }
    }
}
