using ErmayMuhasebe.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.Services
{
    public class FileService : IFileService
    {
        public async Task SaveAndOpenFileAsync(string fileName, byte[] content, string contentType)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ErmayPdf");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                // Ensure unique filename to prevent locking if opened
                string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                string uniqueName = $"{nameWithoutExt}_{DateTime.Now:HHmmss}{ext}";
                
                string safeName = string.Join("_", uniqueName.Split(Path.GetInvalidFileNameChars()));
                string path = Path.Combine(tempDir, safeName);

                await File.WriteAllBytesAsync(path, content);

                try
                {
                    new Process
                    {
                        StartInfo = new ProcessStartInfo(path)
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Fallback: Open folder and select file if no PDF reader is installed
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File Open Error: {ex.Message}");
                // Still open containing folder as absolute fallback
                try 
                { 
                    // Use 'tempDir' or just open Temp
                    var fallbackPath = Path.Combine(Path.GetTempPath(), "ErmayPdf");
                    if (Directory.Exists(fallbackPath))
                        Process.Start("explorer.exe", fallbackPath); 
                } 
                catch { }
            }
        }
        public async Task SaveFileAsync(string fileName, byte[] content)
        {
            try 
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string path = Path.Combine(desktop, fileName);
                
                if (File.Exists(path))
                {
                    try { File.Delete(path); } 
                    catch { path = Path.Combine(desktop, $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:HHmmss}{Path.GetExtension(fileName)}"); }
                }

                await File.WriteAllBytesAsync(path, content);
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public async Task<IFileWrapper?> OpenFilePickerAsync(string title, string[] extensions)
        {
            var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow == null) return null;

            var storage = global::Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow)?.StorageProvider;
            if (storage == null) return null;

            var result = await storage.OpenFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { new global::Avalonia.Platform.Storage.FilePickerFileType(title) { Patterns = extensions.Select(x => "*." + x).ToList() } }
            });

            if (result.Count > 0)
            {
                return new AvaloniaFileWrapper(result[0]);
            }

            return null;
        }

        public async Task<string?> SaveFilePickerAsync(string title, string defaultFileName, string extension)
        {
            var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow == null) return null;

            var storage = global::Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow)?.StorageProvider;
            if (storage == null) return null;

            var result = await storage.SaveFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultFileName,
                DefaultExtension = extension,
                FileTypeChoices = new[] { new global::Avalonia.Platform.Storage.FilePickerFileType(title) { Patterns = new[] { "*." + extension } } }
            });

            return result?.Path.LocalPath;
        }

        public async Task<string?> OpenFolderPickerAsync(string title)
        {
            var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow == null) return null;

            var storage = global::Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow)?.StorageProvider;
            if (storage == null) return null;

            var result = await storage.OpenFolderPickerAsync(new global::Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                return result[0].Path.LocalPath;
            }

            return null;
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow == null) return true;

            var tcs = new TaskCompletionSource<bool>();

            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new global::Avalonia.Controls.Window
                {
                    Title = title,
                    Width = 420,
                    Height = 190,
                    WindowStartupLocation = global::Avalonia.Controls.WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    Background = global::Avalonia.Media.Brush.Parse("#161616"),
                    SystemDecorations = global::Avalonia.Controls.SystemDecorations.Full
                };

                var mainGrid = new global::Avalonia.Controls.Grid
                {
                    Margin = new global::Avalonia.Thickness(20),
                    RowDefinitions = new global::Avalonia.Controls.RowDefinitions("*, Auto")
                };

                var textBlock = new global::Avalonia.Controls.TextBlock
                {
                    Text = message,
                    Foreground = global::Avalonia.Media.Brushes.White,
                    FontSize = 14,
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new global::Avalonia.Thickness(0, 0, 0, 15)
                };
                global::Avalonia.Controls.Grid.SetRow(textBlock, 0);
                mainGrid.Children.Add(textBlock);

                var buttonPanel = new global::Avalonia.Controls.StackPanel
                {
                    Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 12
                };
                global::Avalonia.Controls.Grid.SetRow(buttonPanel, 1);

                var yesButton = new global::Avalonia.Controls.Button
                {
                    Content = "Evet",
                    Width = 85,
                    Height = 36,
                    HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                    Background = global::Avalonia.Media.Brush.Parse("#10B981"),
                    Foreground = global::Avalonia.Media.Brushes.White
                };
                yesButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(true);
                    dialog.Close();
                };

                var noButton = new global::Avalonia.Controls.Button
                {
                    Content = "Hayır",
                    Width = 85,
                    Height = 36,
                    HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                    Background = global::Avalonia.Media.Brush.Parse("#374151"),
                    Foreground = global::Avalonia.Media.Brushes.White
                };
                noButton.Click += (s, e) =>
                {
                    tcs.TrySetResult(false);
                    dialog.Close();
                };

                buttonPanel.Children.Add(yesButton);
                buttonPanel.Children.Add(noButton);
                mainGrid.Children.Add(buttonPanel);

                dialog.Content = mainGrid;

                dialog.Closed += (s, e) =>
                {
                    tcs.TrySetResult(false);
                };

                var activeWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                await dialog.ShowDialog(activeWindow);
            });

            return await tcs.Task;
        }
    }

    public class AvaloniaFileWrapper : IFileWrapper
    {
        private readonly global::Avalonia.Platform.Storage.IStorageFile _file;
        public AvaloniaFileWrapper(global::Avalonia.Platform.Storage.IStorageFile file) => _file = file;

        public string Name => _file.Name;
        public Task<Stream> OpenReadAsync() => _file.OpenReadAsync();
    }
}
