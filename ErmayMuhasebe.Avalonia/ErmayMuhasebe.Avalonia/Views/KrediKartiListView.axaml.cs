using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using System.Linq;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class KrediKartiListView : UserControl
{
    public KrediKartiListView()
    {
        InitializeComponent();
    }

    private async void UploadSlip_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.KrediKartiListViewModel vm)
        {
             var topLevel = TopLevel.GetTopLevel(this);
             if (topLevel == null) return;
             
             var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
             {
                 Title = "Slip Görseli Seç",
                 AllowMultiple = false,
                 FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
             });

             if (files.Count >= 1)
             {
                 vm.EditSlipPath = files[0].Path.LocalPath;
             }
        }
    }
}
