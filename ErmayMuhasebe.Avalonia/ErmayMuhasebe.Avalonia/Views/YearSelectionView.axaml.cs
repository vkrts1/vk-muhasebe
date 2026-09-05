using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ErmayMuhasebe.Avalonia.Views
{
    public partial class YearSelectionView : UserControl
    {
        public YearSelectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
