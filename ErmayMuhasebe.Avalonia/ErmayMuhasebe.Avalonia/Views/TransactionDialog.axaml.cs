using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class TransactionDialog : UserControl
{
    public TransactionDialog()
    {
        InitializeComponent();
    }

    private async void UploadSlip_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.CariListViewModel vm)
        {
             var topLevel = TopLevel.GetTopLevel(this);
             if (topLevel == null) return;
             
             var isEft = vm.IsHavaleEFT;
             var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
             {
                 Title = isEft ? "Dekont Dosyası Seç" : "Slip Görseli Seç",
                 AllowMultiple = false,
                 FileTypeFilter = new[] { FilePickerFileTypes.ImageAll, FilePickerFileTypes.Pdf }
             });

             if (files.Count >= 1)
             {
                 vm.KkSlipPath = files[0].Path.LocalPath;
             }
        }
    }
}
