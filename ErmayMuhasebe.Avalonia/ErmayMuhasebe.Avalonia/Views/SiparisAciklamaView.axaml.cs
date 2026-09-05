using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class SiparisAciklamaView : UserControl
{
    public SiparisAciklamaView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
