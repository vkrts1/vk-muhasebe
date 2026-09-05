using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class HaftalikHedefWrapper : ObservableObject
{
    public HaftalikSatisHedefi Model { get; }
    public decimal Gerceklesen { get; }
    public decimal Yuzde => Model.HedefTutari > 0 ? (Gerceklesen / Model.HedefTutari) * 100 : 0;
    
    public string HaftaAdi => $"{Model.Hafta}. Hafta ({GetDateRange()})";
    
    // Status Color
    public string Renk => Yuzde >= 100 ? "#10B981" : (Yuzde >= 70 ? "#3B82F6" : (Yuzde >= 50 ? "#F59E0B" : "#EF4444"));

    public HaftalikHedefWrapper(HaftalikSatisHedefi model, decimal gerceklesen)
    {
        Model = model;
        Gerceklesen = gerceklesen;
    }

    private string GetDateRange()
    {
        try 
        {
            var firstDate = ISOWeek.ToDateTime(Model.Yil, Model.Hafta, DayOfWeek.Monday);
            var lastDate = firstDate.AddDays(6);
            return $"{firstDate:dd MMM} - {lastDate:dd MMM}";
        }
        catch { return ""; }
    }
}

public partial class HaftalikHedefTakipViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;

    [ObservableProperty] private ObservableCollection<HaftalikHedefWrapper> _hedefler = new();
    [ObservableProperty] private HaftalikHedefWrapper? _selectedHedef;

    [ObservableProperty] private bool _isYearListVisible = true;
    [ObservableProperty] private bool _isWeekListVisible = false;
    [ObservableProperty] private ObservableCollection<int> _availableYears = new();
    
    // Filter
    [ObservableProperty] private int _selectedFilterYil = DateTime.Now.Year;

    // --- Edit Properties ---
    [ObservableProperty] private bool _isEditFormVisible;
    [ObservableProperty] private int _editYil = DateTime.Now.Year;
    [ObservableProperty] private int _editHafta = ISOWeek.GetWeekOfYear(DateTime.Now);
    [ObservableProperty] private decimal _editHedefTutar;
    [ObservableProperty] private string _editTitle = "Yeni Hedef Kaydı";

    public HaftalikHedefTakipViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var hedefler = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();
        
        // Populate available years from DB + Current Year + Next Year as default
        var years = hedefler.Select(h => h.Yil).Distinct().ToList();
        if(!years.Contains(DateTime.Now.Year)) years.Add(DateTime.Now.Year);
        
        AvailableYears = new ObservableCollection<int>(years.OrderByDescending(y => y));
        
        // If we are currently in week view, refresh the weeks
        if (IsWeekListVisible)
        {
             await LoadWeeksForYear(SelectedFilterYil);
        }
    }
    
    private async Task LoadWeeksForYear(int year)
    {
        var hedefler = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();
        var i1 = await _uow.Faturalar.GetByTurAsync("Satis");
        var i2 = await _uow.Faturalar.GetByTurAsync("Satış");
        var salesInvoices = i1.Concat(i2).ToList();

        var list = new List<HaftalikHedefWrapper>();
        var yearTargets = hedefler.Where(h => h.Yil == year).ToList();

        foreach (var h in yearTargets)
        {
            var invTotal = salesInvoices
                .Where(f => ISOWeek.GetYear(f.Tarih) == h.Yil && ISOWeek.GetWeekOfYear(f.Tarih) == h.Hafta)
                .Sum(f => f.GenelToplam);

            list.Add(new HaftalikHedefWrapper(h, invTotal));
        }

        Hedefler = new ObservableCollection<HaftalikHedefWrapper>(list.OrderBy(x => x.Model.Hafta));
    }

    [RelayCommand]
    public async Task SelectYear(int year)
    {
        SelectedFilterYil = year;
        await LoadWeeksForYear(year);
        IsYearListVisible = false;
        IsWeekListVisible = true;
    }
    
    [RelayCommand]
    public void BackToYears()
    {
        IsWeekListVisible = false;
        IsYearListVisible = true;
        _ = LoadDataAsync(); // Refresh years list
    }

    public List<int> Haftalar { get; } = Enumerable.Range(1, 53).ToList();

    [RelayCommand]
    public async Task CreateYearlyTargetsAsync()
    {
        int yearToCreate = SelectedFilterYil;
        int weeksInYear = ISOWeek.GetWeeksInYear(yearToCreate);
        
        var existingTasks = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();
        
        for (int w = 1; w <= weeksInYear; w++)
        {
            if (!existingTasks.Any(x => x.Yil == yearToCreate && x.Hafta == w))
            {
                var h = new HaftalikSatisHedefi 
                { 
                    Yil = yearToCreate, 
                    Hafta = w, 
                    HedefTutari = 0 
                };
                await _uow.Hedefler.SaveHaftalikHedefAsync(h);
            }
        }
        
        await LoadDataAsync(); 
        await SelectYear(yearToCreate); 
    }

    [RelayCommand]
    public void OpenNewForm()
    {
        SelectedHedef = null;
        EditTitle = "Yeni Hedef Kaydı";
        EditYil = SelectedFilterYil; 
        EditHafta = ISOWeek.GetWeekOfYear(DateTime.Now);
        EditHedefTutar = 0;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    public void OpenEditForm(HaftalikHedefWrapper? w)
    {
        if (w == null) return;
        SelectedHedef = w;
        EditTitle = "Hedef Düzenle";
        EditYil = w.Model.Yil;
        EditHafta = w.Model.Hafta;
        EditHedefTutar = w.Model.HedefTutari;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        var existingTasks = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();
        var duplicate = existingTasks.FirstOrDefault(x => x.Yil == EditYil && x.Hafta == EditHafta);
        var h = SelectedHedef?.Model;

        if (h == null)
        {
            if (duplicate != null) h = duplicate;
            else h = new HaftalikSatisHedefi();
        }

        h.Yil = EditYil;
        h.Hafta = EditHafta;
        h.HedefTutari = EditHedefTutar;

        await _uow.Hedefler.SaveHaftalikHedefAsync(h);
        IsEditFormVisible = false;
        
        // Refresh Current View
        if(IsWeekListVisible) await LoadWeeksForYear(SelectedFilterYil);
        else await LoadDataAsync();
    }

    [RelayCommand]
    public async Task DeleteAsync(HaftalikHedefWrapper? w)
    {
        if (w == null) return;
        await _uow.Hedefler.DeleteHaftalikHedefAsync(w.Model.Id);
        
        // Refresh
        if(IsWeekListVisible) await LoadWeeksForYear(SelectedFilterYil);
    }

    [RelayCommand]
    public void CloseForm() => IsEditFormVisible = false;
}
