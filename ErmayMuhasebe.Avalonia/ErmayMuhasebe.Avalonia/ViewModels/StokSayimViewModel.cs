using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class StokSayimViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;

    [ObservableProperty]
    private ObservableCollection<StokSayimFisi> _fisler = new();

    [ObservableProperty]
    private StokSayimFisi? _selectedFis;

    [ObservableProperty]
    private ObservableCollection<StokSayimDetay> _selectedFisDetaylar = new();

    public StokSayimViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadFislerAsync();
    }

    [RelayCommand]
    public async Task LoadFislerAsync()
    {
        var list = await _uow.StokSayimlar.GetAllAsync();
        Fisler = new ObservableCollection<StokSayimFisi>(list);
    }
    
    partial void OnSelectedFisChanged(StokSayimFisi? value)
    {
        if (value != null)
        {
            _ = LoadDetaylarAsync(value.Id);
        }
        else
        {
            SelectedFisDetaylar.Clear();
        }
    }

    private async Task LoadDetaylarAsync(int fisId)
    {
        var detaylar = await _uow.StokSayimlar.GetDetaylarAsync(fisId);
        SelectedFisDetaylar = new ObservableCollection<StokSayimDetay>(detaylar);
    }
}
