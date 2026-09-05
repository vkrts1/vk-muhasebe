using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ErmayMuhasebe.Avalonia.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class BelgeArsivView : UserControl
{
    public BelgeArsivView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is BelgeArsivViewModel vm)
        {
            vm.FilePicker = async () => 
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return null;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Arşivlenecek Belgeyi Seçin",
                    AllowMultiple = false
                });

                return files.FirstOrDefault()?.Path.LocalPath;
            };

            vm.SaveFilePicker = async (filename) =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return null;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Dosyayı Kaydet",
                    SuggestedFileName = filename
                });

                return file?.Path.LocalPath;
            };
        }
    }
}
