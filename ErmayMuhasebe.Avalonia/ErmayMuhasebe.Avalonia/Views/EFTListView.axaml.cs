using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ErmayMuhasebe.Avalonia.ViewModels;
using System.Collections.Generic;
using System;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class EFTListView : UserControl
{
    public EFTListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void UploadDekont_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EFTListViewModel vm)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Dekont Seç",
                AllowMultiple = false,
                FileTypeFilter = new[] { 
                    new FilePickerFileType("Belgeler") { Patterns = new[] { "*.jpg", "*.png", "*.pdf" } } 
                }
            });

            if (files != null && files.Count > 0)
            {
                vm.EditDekontPath = files[0].Path.LocalPath;
            }
        }
    }

    private void OnCariDoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is EFTListViewModel vm && vm.SelectedCariInList != null)
        {
            vm.SelectCariCommand.Execute(vm.SelectedCariInList);
        }
    }
}
