using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ErmayMuhasebe.Avalonia.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.Views
{
    public partial class MusteriTakipDetayView : UserControl
    {
        public MusteriTakipDetayView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is MusteriTakipDetayViewModel vm)
            {
                vm.ImagePickerAction = OpenImagePickerAsync;
                vm.OpenImageExternalAction = OpenImageFileExternally;
            }
        }

        private async Task<string?> OpenImagePickerAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Müşteri İçin Görsel Seçin",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Görsel Dosyaları")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.pdf" }
                    },
                    FilePickerFileTypes.All
                }
            });

            if (files.Count > 0)
            {
                return files[0].Path.LocalPath;
            }

            return null;
        }

        private void OpenImageFileExternally(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    new Process
                    {
                        StartInfo = new ProcessStartInfo(path)
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MusteriTakipDetayView] OpenImageFileExternally Error: {ex.Message}");
            }
        }
    }
}
