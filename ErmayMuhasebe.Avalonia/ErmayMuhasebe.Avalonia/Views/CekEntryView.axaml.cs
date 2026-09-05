using Avalonia.Controls;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class CekEntryView : UserControl
{
    public CekEntryView()
    {
        InitializeComponent();
    }

    private void OnCariDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.CekSenetListViewModel vm && vm.SelectedCariForAdd != null)
        {
            vm.SelectCariCommand.Execute(null);
        }
    }
}
