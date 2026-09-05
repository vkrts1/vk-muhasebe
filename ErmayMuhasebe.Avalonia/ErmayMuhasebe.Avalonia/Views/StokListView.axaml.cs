using Avalonia.Controls;
using Avalonia.Input;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class StokListView : UserControl
{
    public StokListView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.StokListViewModel vm && vm.SelectedStok != null)
        {
            // Usually double-clicking a list item should trigger an edit or detail view.
            // In StokListViewModel, Selecting a stock already populates the edit form on the right.
            // If there's a specific 'Edit' command, we could call it here.
            // For consistency with other areas, let's ensure it's selected.
            // The UI already binds SelectedItem, so it's already selected.
        }
    }
}
