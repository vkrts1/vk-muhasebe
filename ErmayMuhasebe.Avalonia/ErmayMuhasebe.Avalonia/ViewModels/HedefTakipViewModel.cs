using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class HedefWrapper : ObservableObject
{
    public SatisHedefi Model { get; }
    public decimal Gerceklesen { get; }
    public decimal Yuzde => Model.HedefTutari > 0 ? (Gerceklesen / Model.HedefTutari) * 100 : 0;
    
    public string AyAdi 
    {
        get
        {
            try { return System.Globalization.CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(Model.Ay); }
            catch { return System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Model.Ay); }
        }
    }
    
    // Status Color
    public string Renk => Yuzde >= 100 ? "#10B981" : (Yuzde >= 70 ? "#3B82F6" : (Yuzde >= 50 ? "#F59E0B" : "#EF4444"));

    public HedefWrapper(SatisHedefi model, decimal gerceklesen)
    {
        Model = model;
        Gerceklesen = gerceklesen;
    }
}

public partial class HedefTakipViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;

    [ObservableProperty] private ObservableCollection<HedefWrapper> _hedefler = new();
    [ObservableProperty] private HedefWrapper? _selectedHedef;

    [ObservableProperty] private bool _isYearListVisible = true;
    [ObservableProperty] private bool _isMonthListVisible = false;
    [ObservableProperty] private ObservableCollection<int> _availableYears = new();
    
    // Filter
    [ObservableProperty] private int _selectedFilterYil = DateTime.Now.Year;

    // --- Edit Properties ---
    [ObservableProperty] private bool _isEditFormVisible;
    [ObservableProperty] private int _editYil = DateTime.Now.Year;
    [ObservableProperty] private int _editAy = DateTime.Now.Month;
    [ObservableProperty] private decimal _editHedefTutar;
    [ObservableProperty] private string _editTitle = "Yeni Hedef Kaydı";

    public HedefTakipViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var hedefler = await _uow.Hedefler.GetAllAylikHedeflerAsync();
        
        // Populate available years from DB + Current Year + Next Year as default
        var years = hedefler.Select(h => h.Yil).Distinct().ToList();
        if(!years.Contains(DateTime.Now.Year)) years.Add(DateTime.Now.Year);
        
        AvailableYears = new ObservableCollection<int>(years.OrderByDescending(y => y));
        
        // If we are currently in monthly view, refresh the months
        if (IsMonthListVisible)
        {
             await LoadMonthsForYear(SelectedFilterYil);
        }
    }
    
    private async Task LoadMonthsForYear(int year)
    {
        var hedefler = await _uow.Hedefler.GetAllAylikHedeflerAsync();
        var i1 = await _uow.Faturalar.GetByTurAsync("Satis");
        var i2 = await _uow.Faturalar.GetByTurAsync("Satış");
        var salesInvoices = i1.Concat(i2).ToList();

        var list = new List<HedefWrapper>();
        var yearTargets = hedefler.Where(h => h.Yil == year).ToList();

        foreach (var h in yearTargets)
        {
            var total = salesInvoices
                .Where(f => f.Tarih.Year == h.Yil && f.Tarih.Month == h.Ay)
                .Sum(f => f.GenelToplam);

            list.Add(new HedefWrapper(h, total));
        }

        Hedefler = new ObservableCollection<HedefWrapper>(list.OrderBy(x => x.Model.Ay));
    }

    partial void OnSelectedFilterYilChanged(int value)
    {
        // No-op for now unless we want auto-selection
    }
    
    [RelayCommand]
    public async Task SelectYear(int year)
    {
        SelectedFilterYil = year;
        await LoadMonthsForYear(year);
        IsYearListVisible = false;
        IsMonthListVisible = true;
    }
    
    [RelayCommand]
    public void BackToYears()
    {
        IsMonthListVisible = false;
        IsYearListVisible = true;
        _ = LoadDataAsync(); // Refresh years list
    }

    public List<string> Aylar { get; } = new() 
    { 
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", 
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" 
    };

    [ObservableProperty] private int _selectedAyIndex = DateTime.Now.Month - 1;

    [RelayCommand]
    public async Task CreateYearlyTargetsAsync()
    {
        int yearToCreate = SelectedFilterYil;
        
        // Generate empty targets for the selected filter year if they don't exist
        var existingTasks = await _uow.Hedefler.GetAllAylikHedeflerAsync();
        
        for (int m = 1; m <= 12; m++)
        {
            if (!existingTasks.Any(x => x.Yil == yearToCreate && x.Ay == m))
            {
                var h = new SatisHedefi 
                { 
                    Yil = yearToCreate, 
                    Ay = m, 
                    HedefTutari = 0 
                };
                await _uow.Hedefler.SaveAylikHedefAsync(h);
            }
        }
        
        await LoadDataAsync(); // Refresh years list
        
        // Automatically enter the year if created
        // await SelectYearAsync(yearToCreate); 
    }

    [RelayCommand]
    public void OpenNewForm()
    {
        SelectedHedef = null;
        EditTitle = "Yeni Hedef Kaydı";
        EditYil = SelectedFilterYil; 
        SelectedAyIndex = DateTime.Now.Month - 1;
        EditHedefTutar = 0;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    public void OpenEditForm(HedefWrapper? w)
    {
        if (w == null) return;
        SelectedHedef = w;
        EditTitle = "Hedef Düzenle";
        EditYil = w.Model.Yil;
        SelectedAyIndex = (w.Model.Ay > 0 && w.Model.Ay <= 12) ? w.Model.Ay - 1 : 0;
        EditHedefTutar = w.Model.HedefTutari;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        int targetAy = SelectedAyIndex + 1;
        var existingTasks = await _uow.Hedefler.GetAllAylikHedeflerAsync();
        var duplicate = existingTasks.FirstOrDefault(x => x.Yil == EditYil && x.Ay == targetAy);
        var h = SelectedHedef?.Model;

        if (h == null)
        {
            if (duplicate != null) h = duplicate;
            else h = new SatisHedefi();
        }

        h.Yil = EditYil;
        h.Ay = targetAy;
        h.HedefTutari = EditHedefTutar;

        await _uow.Hedefler.SaveAylikHedefAsync(h);
        IsEditFormVisible = false;
        
        // Refresh Current View
        if(IsMonthListVisible) await LoadMonthsForYear(SelectedFilterYil);
        else await LoadDataAsync();
    }

    [RelayCommand]
    public async Task DeleteAsync(HedefWrapper? w)
    {
        if (w == null) return;
        await _uow.Hedefler.DeleteAylikHedefAsync(w.Model.Id);
        
        // Refresh
        if(IsMonthListVisible) await LoadMonthsForYear(SelectedFilterYil);
    }

    [RelayCommand]
    public void CloseForm() => IsEditFormVisible = false;
}
