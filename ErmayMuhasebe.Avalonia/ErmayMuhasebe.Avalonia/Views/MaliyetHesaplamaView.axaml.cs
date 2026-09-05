using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class MaliyetHesaplamaView : UserControl
{
    public MaliyetHesaplamaView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
