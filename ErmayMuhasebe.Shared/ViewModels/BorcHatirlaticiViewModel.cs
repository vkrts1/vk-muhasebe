using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Shared.ViewModels;

public record BorcHatirlatitmaItem(string CariUnvan, decimal Tutar, DateTime Vade, int GecikmeGunu, string Durum, string Telefon);

public abstract partial class BorcHatirlaticiViewModel : ViewModelBase
{
    protected readonly IUnitOfWork _uow;

    [ObservableProperty] private ObservableCollection<BorcHatirlatitmaItem> _hatirlatmalar = new();
    [ObservableProperty] private int _toplamGecikenCount;
    [ObservableProperty] private decimal _toplamGecikenTutar;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _successMessage = "";

    public BorcHatirlaticiViewModel(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public virtual async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            var cariler = await _uow.Cariler.GetAllAsync();
            var list = new List<BorcHatirlatitmaItem>();
            
            // Simulating debt retrieval (Logic from Desktop)
            // In a real system, this would look at invoice due dates.
            // For parity, we use the desktop logic: Balance > 0 and simulated overdue.
            foreach (var c in cariler.Where(x => (x.Borc - x.Alacak) > 0))
            {
                var bakiye = c.Borc - c.Alacak;
                // Simulated overdue: Today - 10 days
                var simulatedVade = DateTime.Now.AddDays(-14); 
                int gecikme = (DateTime.Now - simulatedVade).Days;
                
                list.Add(new BorcHatirlatitmaItem(
                    c.Unvan ?? "İsimsiz Cari", 
                    bakiye, 
                    simulatedVade, 
                    gecikme, 
                    "VAADESİ GEÇTİ", 
                    c.Telefon ?? "Girilmemiş"));
            }

            await InvokeOnUIThreadAsync(() => {
                Hatirlatmalar = new ObservableCollection<BorcHatirlatitmaItem>(list.OrderByDescending(x => x.GecikmeGunu));
                ToplamGecikenCount = list.Count;
                ToplamGecikenTutar = list.Sum(x => x.Tutar);
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Veri yükleme hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public abstract Task SendSmsAsync(BorcHatirlatitmaItem item);

    [RelayCommand]
    public abstract Task SendEmailAsync(BorcHatirlatitmaItem item);
}
