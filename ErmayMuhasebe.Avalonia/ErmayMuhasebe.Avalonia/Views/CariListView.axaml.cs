using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity; // Added
using System.Linq;
using Microsoft.Extensions.DependencyInjection; // Added

namespace ErmayMuhasebe.Avalonia.Views;

public partial class CariListView : UserControl
{
    public CariListView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.CariListViewModel vm && vm.SelectedCari != null)
        {
            // Cari için detay penceresini açar
            var app = global::Avalonia.Application.Current as App;
            var uow = app?.Services?.GetService<ErmayMuhasebe.Repositories.IUnitOfWork>();
            var pdf = app?.Services?.GetService<ErmayMuhasebe.Services.PdfService>();

            if (uow != null && pdf != null)
            {
                var detailVm = new ViewModels.CariDetayViewModel(uow, pdf);
                _ = detailVm.InitializeAsync(vm.SelectedCari.Id);

                var window = new CariDetayWindow
                {
                    DataContext = detailVm
                };
                window.Show();
            }
        }
    }

    private void OnHareketDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.CariListViewModel vm && vm.SelectedHareket != null)
        {
            // İşlem detayı/düzenleme ekranını açar
            _ = vm.EditTransactionAsync();
        }
    }
}
