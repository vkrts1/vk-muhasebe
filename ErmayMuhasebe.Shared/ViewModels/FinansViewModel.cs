using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class FinansViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ErmayMuhasebe.Shared.ViewModels.ViewModelBase? _currentFinansPage;

    public ErmayMuhasebe.Shared.ViewModels.ViewModelBase DashboardViewModel { get; }
    public ErmayMuhasebe.Shared.ViewModels.ViewModelBase NakitViewModel { get; }
    public ErmayMuhasebe.Shared.ViewModels.ViewModelBase CekViewModel { get; }
    public ErmayMuhasebe.Shared.ViewModels.ViewModelBase KrediKartiViewModel { get; }
    public ErmayMuhasebe.Shared.ViewModels.ViewModelBase EFTViewModel { get; }


    public FinansViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Mevcut ViewModel'leri alıyoruz
        DashboardViewModel = _serviceProvider.GetRequiredService<FinansDashboardViewModel>();
        NakitViewModel = _serviceProvider.GetRequiredService<KasaListViewModel>();
        CekViewModel = _serviceProvider.GetRequiredService<CekSenetListViewModel>();
        KrediKartiViewModel = _serviceProvider.GetRequiredService<KrediKartiListViewModel>();
        EFTViewModel = _serviceProvider.GetRequiredService<EFTListViewModel>();


        // Varsayılan sayfa Dashboard olsun
        CurrentFinansPage = DashboardViewModel;

        if (NakitViewModel is KasaListViewModel kasaList)
        {
            kasaList.KasaSelected += async (kasa) => 
            {
                var detayVm = _serviceProvider.GetRequiredService<KasaDetayViewModel>();
                await detayVm.InitializeAsync(kasa);
                detayVm.GoBackRequest += () => CurrentFinansPage = NakitViewModel;
                CurrentFinansPage = detayVm;
            };
        }
    }

    public override void OnNavigatedTo()
    {
        DashboardViewModel?.OnNavigatedTo();
        NakitViewModel?.OnNavigatedTo();
        CekViewModel?.OnNavigatedTo();
        KrediKartiViewModel?.OnNavigatedTo();
        EFTViewModel?.OnNavigatedTo();

    }

    private IUnitOfWork _uow => _serviceProvider.GetRequiredService<IUnitOfWork>();

    [RelayCommand]
    public void SetPage(string pageName)
    {
        var newPage = pageName switch
        {
            "Dashboard" => DashboardViewModel,
            "Nakit" => NakitViewModel,
            "Cek" => CekViewModel,
            "KrediKarti" => KrediKartiViewModel,
            "EFT" => EFTViewModel,
            _ => DashboardViewModel
        };

        if (CurrentFinansPage != newPage)
        {
            CurrentFinansPage = newPage;
            CurrentFinansPage?.OnNavigatedTo();
        }
    }

    partial void OnCurrentFinansPageChanged(ErmayMuhasebe.Shared.ViewModels.ViewModelBase? value)
    {
        value?.OnNavigatedTo();
    }
}
