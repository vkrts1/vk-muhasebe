using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

namespace ErmayMuhasebe.Shared.ViewModels;

// Wrapper for Monthly Display
public partial class AylikPlanWrapper : ObservableObject
{
    [ObservableProperty] private int _ay;
    [ObservableProperty] private string _ayAdi;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(Fark))]
    [NotifyPropertyChangedFor(nameof(Yuzde))]
    [NotifyPropertyChangedFor(nameof(Renk))]
    private decimal _hedef;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(Fark))]
    [NotifyPropertyChangedFor(nameof(Yuzde))]
    [NotifyPropertyChangedFor(nameof(Renk))]
    private decimal _gerceklesen;
    
    public decimal Fark => Gerceklesen - Hedef;
    public decimal Yuzde => Hedef > 0 ? Math.Round((Gerceklesen / Hedef) * 100, 1) : 0;
    public string Renk => Yuzde >= 100 ? "#10B981" : (Yuzde >= 70 ? "#3B82F6" : "#EF4444");

    public AylikPlanWrapper(int ay, decimal hedef, decimal gerceklesen)
    {
        Ay = ay;
        try { AyAdi = CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(ay); }
        catch { AyAdi = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(ay); }
        Hedef = hedef;
        Gerceklesen = gerceklesen;
    }
}

public partial class HaftalikPlanWrapper : ObservableObject
{
    [ObservableProperty] private int _hafta; // ISO Week 1-53
    [ObservableProperty] private string _haftaAdi;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(Fark))]
    [NotifyPropertyChangedFor(nameof(Yuzde))]
    [NotifyPropertyChangedFor(nameof(Renk))]
    private decimal _hedef;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(Fark))]
    [NotifyPropertyChangedFor(nameof(Yuzde))]
    [NotifyPropertyChangedFor(nameof(Renk))]
    private decimal _gerceklesen;

    public decimal Fark => Gerceklesen - Hedef;
    public decimal Yuzde => Hedef > 0 ? Math.Round((Gerceklesen / Hedef) * 100, 1) : 0;
    public string Renk => Yuzde >= 100 ? "#10B981" : (Yuzde >= 70 ? "#3B82F6" : (Yuzde >= 50 ? "#F59E0B" : "#EF4444"));

    public HaftalikPlanWrapper(int hafta, int year, decimal hedef, decimal gerceklesen)
    {
        Hafta = hafta; // Used for saving
        Hedef = hedef;
        Gerceklesen = gerceklesen;
        
        // Calculate date range for display
        try 
        {
             var firstDate = ISOWeek.ToDateTime(year, hafta, DayOfWeek.Monday);
             var lastDate = firstDate.AddDays(6);
             HaftaAdi = $"{hafta}. Hafta ({firstDate:dd MMM} - {lastDate:dd MMM})";
        }
        catch { HaftaAdi = $"{hafta}. Hafta"; }
    }
}

public partial class ButcePlanlamaViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    private readonly IYearContext _yearContext;

    // --- State ---
    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private ObservableCollection<int> _availableYears = new();
    
    // Yillik Data
    [ObservableProperty] private decimal _yillikHedef;
    [ObservableProperty] private decimal _yillikGerceklesen;
    [ObservableProperty] private decimal _yillikYuzde;
    
    // Tablo Data
    [ObservableProperty] private ObservableCollection<AylikPlanWrapper> _aylikPlanlar = new();
    
    // Weekly Selection
    [ObservableProperty] private AylikPlanWrapper? _selectedAylikPlan;
    [ObservableProperty] private ObservableCollection<HaftalikPlanWrapper> _haftalikPlanlar = new();
    [ObservableProperty] private bool _isWeeklyVisible;

    // Chart Data (Generic for both platforms)
    [ObservableProperty] private List<double> _trendValuesA = new();
    [ObservableProperty] private List<double> _trendValuesB = new();
    [ObservableProperty] private List<string> _trendLabels = new();

    public ButcePlanlamaViewModel(IUnitOfWork uow, IYearContext yearContext)
    {
        _uow = uow;
        _yearContext = yearContext;
        StatusMessage = "";
        
        ScanAvailableYears();
        
        // 1. Initial Empty Data to avoid empty grid issues
        var list = new List<AylikPlanWrapper>();
        for (int m = 1; m <= 12; m++) list.Add(new AylikPlanWrapper(m, 0, 0));
        AylikPlanlar = new ObservableCollection<AylikPlanWrapper>(list);

        try 
        {
            InitializeChart();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeChart Error: {ex.Message}");
        }
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = LoadDataAsync();
    }

    private void InitializeChart()
    {
        TrendLabels = GetMonthNames();
    }

    private List<string> GetMonthNames()
    {
        try { return CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.AbbreviatedMonthNames.Take(12).ToList(); }
        catch { return CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12).ToList(); }
    }

    private void ScanAvailableYears()
    {
        int currentSysYear = _yearContext.CurrentYear > 0 ? _yearContext.CurrentYear : DateTime.Now.Year;
        var years = new List<int>();

        try
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe");
            if (Directory.Exists(appData))
            {
                var files = Directory.GetFiles(appData, "ermay_*.db");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var parts = fileName.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int year))
                    {
                        if (!years.Contains(year)) years.Add(year);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Year Scan Error: {ex.Message}");
        }

        if (!years.Any()) years.Add(currentSysYear);

        AvailableYears = new ObservableCollection<int>(years.OrderByDescending(x => x));
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            // Update years from filesystem on each load
            ScanAvailableYears();
            
            // Just ensure selection is synced
            SelectedYear = _yearContext.CurrentYear > 0 ? _yearContext.CurrentYear : DateTime.Now.Year;

            // 2. Load Selected Year Data
            await LoadYearData(SelectedYear);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading budget data: {ex.Message}");
        }
    }

    private async Task LoadYearData(int year)
    {
        try 
        {
            // A. Yillik Hedef
            var yillikTable = await _uow.Hedefler.GetAllYillikHedeflerAsync();
            var currentYillik = yillikTable.FirstOrDefault(x => x.Yil == year);
            YillikHedef = currentYillik?.HedefTutari ?? 0;

            // B. Faturalar (Gerceklesen)
            var s1 = await _uow.Faturalar.GetByTurAsync("Satis");
            var s2 = await _uow.Faturalar.GetByTurAsync("Satış");
            var sales = s1.Concat(s2).Where(f => f.Tarih.Year == year).ToList();
            
            YillikGerceklesen = sales.Sum(x => x.GenelToplam);
            YillikYuzde = YillikHedef > 0 ? (YillikGerceklesen / YillikHedef) * 100 : 0;

            // C. Aylik Hedefler (Alt Kirilim)
            var aylikHedeflerTable = await _uow.Hedefler.GetAllAylikHedeflerAsync();
            var yearMonthlyTargets = aylikHedeflerTable.Where(x => x.Yil == year).ToList();

            var list = new List<AylikPlanWrapper>();
            var targetValues = new List<double>();
            var actualValues = new List<double>();

            for (int m = 1; m <= 12; m++)
            {
                decimal target = yearMonthlyTargets.FirstOrDefault(x => x.Ay == m)?.HedefTutari ?? 0;
                decimal actual = sales.Where(x => x.Tarih.Month == m).Sum(x => x.GenelToplam);
                
                list.Add(new AylikPlanWrapper(m, target, actual));
                
                targetValues.Add((double)target);
                actualValues.Add((double)actual);
            }

            await InvokeOnUIThreadAsync(() => {
                // Ensure we have exactly 12 items
                if (AylikPlanlar.Count != 12)
                {
                    AylikPlanlar = new ObservableCollection<AylikPlanWrapper>(list);
                }
                else
                {
                    for (int i = 0; i < 12; i++)
                    {
                        AylikPlanlar[i].Hedef = list[i].Hedef;
                        AylikPlanlar[i].Gerceklesen = list[i].Gerceklesen;
                    }
                }

                TrendLabels = GetMonthNames();
                TrendValuesA = targetValues;
                TrendValuesB = actualValues;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadYearData Error: {ex.Message}");
        }
    }

    partial void OnSelectedYearChanged(int value)
    {
        _ = LoadYearData(value);
    }

    [RelayCommand]
    public async Task ChangeYear(int year)
    {
        SelectedYear = year;
        await LoadYearData(year);
    }

    partial void OnSelectedAylikPlanChanging(AylikPlanWrapper? value)
    {
        if (value != null) value.PropertyChanged -= OnMonthlyPlanPropertyChanged;
    }

    async partial void OnSelectedAylikPlanChanged(AylikPlanWrapper? value)
    {
        if (value != null) value.PropertyChanged += OnMonthlyPlanPropertyChanged;

        try 
        {
            if (value == null)
            {
                IsWeeklyVisible = false;
                // Revert chart to Yearly View
                await LoadYearData(SelectedYear); 
                return;
            }

            IsWeeklyVisible = true;
            await LoadWeeklyData(value.Ay);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Selection Change Error: {ex.Message}");
        }
    }

    private void OnMonthlyPlanPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AylikPlanWrapper.Hedef))
        {
             if (SelectedAylikPlan != null)
             {
                 _ = LoadWeeklyData(SelectedAylikPlan.Ay);
             }
        }
    }

    private async Task LoadWeeklyData(int month)
    {
        try 
        {
            var year = SelectedYear;
            
            // 1. Calculate which ISO weeks intersect with this month
            var weeksInMonth = GetWeeksInMonth(year, month);

        // 2. Fetch Data
        var weeklyTargets = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();
        var i1 = await _uow.Faturalar.GetByTurAsync("Satis");
        var i2 = await _uow.Faturalar.GetByTurAsync("Satış");
        var sales = i1.Concat(i2)
            .Where(f => f.Tarih.Year == year)
            .ToList();

            var list = new List<HaftalikPlanWrapper>();

            // Smart Auto-Distribution Logic
            var currentMonthPlan = AylikPlanlar.FirstOrDefault(x => x.Ay == month);
            decimal memoryMonthlyTarget = currentMonthPlan?.Hedef ?? 0;
            
            var dbTargetsForMonth = weeklyTargets.Where(x => x.Yil == year && weeksInMonth.Contains(x.Hafta)).ToList();
            decimal dbSum = dbTargetsForMonth.Sum(x => x.HedefTutari);
            
            // If the DB data matches our current in-memory monthly target, we trust the DB (user might have manually adjusted weeks).
            // If they differ (e.g. user changed monthly target but didn't save yet), we auto-distribute.
            bool useDbValues = Math.Abs(dbSum - memoryMonthlyTarget) < 1.0m && memoryMonthlyTarget > 0;
            decimal autoPerWeek = weeksInMonth.Count > 0 ? Math.Round(memoryMonthlyTarget / weeksInMonth.Count, 2) : 0;

            foreach (var wk in weeksInMonth)
            {
                decimal targetVal;
                
                if (useDbValues)
                {
                    targetVal = dbTargetsForMonth.FirstOrDefault(x => x.Hafta == wk)?.HedefTutari ?? 0;
                }
                else
                {
                    targetVal = autoPerWeek;
                }

                // Actual Sales for this week
                decimal actual = sales
                    .Where(f => ISOWeek.GetWeekOfYear(f.Tarih) == wk)
                    .Sum(x => x.GenelToplam);

                list.Add(new HaftalikPlanWrapper(wk, year, targetVal, actual));
            }

        var wLabels = list.Select(x => $"{x.Hafta}.H").ToList();
        var wTarget = list.Select(x => (double)x.Hedef).ToList();
        var wActual = list.Select(x => (double)x.Gerceklesen).ToList();

        await InvokeOnUIThreadAsync(() => {
            HaftalikPlanlar = new ObservableCollection<HaftalikPlanWrapper>(list);
            
            TrendLabels = wLabels;
            TrendValuesA = wTarget;
            TrendValuesB = wActual;
        });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadWeeklyData Error: {ex.Message}");
        }
    }

    // --- Actions ---
    [RelayCommand]
    public async Task UpdateYillikHedef()
    {
        try 
        {
            // --- UI-ONLY UPDATE FOR IMMEDIATE FEEDBACK ---
            if (YillikHedef <= 0) return;
            
            decimal monthlyTarget = Math.Round(YillikHedef / 12, 2);
            
            await InvokeOnUIThreadAsync(() => {
                foreach (var item in AylikPlanlar)
                {
                    item.Hedef = monthlyTarget;
                }
            });
            
            // Note: We don't save to DB here yet. The user will see the change 
            // and can then click "Kaydet" to persist everything.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateYillikHedef Error: {ex.Message}");
        }
    }
    
    [ObservableProperty] private bool _isEditMode;
    [RelayCommand]
    public void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
    }
    
    [ObservableProperty] private string _statusMessage;
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    public async Task SaveAylikPlanlar()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Kayıt Başlatılıyor...";
        await Task.Delay(100);

        try 
        {
            var year = SelectedYear;
            var targetToSave = YillikHedef; 
            var currentPlans = AylikPlanlar.ToList(); 
            
            // 0. Tablo/Veri Kontrolü
            // Note: Tables are created in DatabaseService.InitializeAsync. 
            // We rely on service methods which strictly call EnsureInitializedAsync internally.

            // 1. Yıllık Hedef
            StatusMessage = "Yıllık hedef kaydediliyor...";
            var yList = await _uow.Hedefler.GetAllYillikHedeflerAsync();
            var yExisting = yList.FirstOrDefault(x => x.Yil == year) ?? new YillikSatisHedefi { Yil = year };
            yExisting.HedefTutari = targetToSave;
            await _uow.Hedefler.SaveYillikHedefAsync(yExisting);

            // 2. Hazırlık
            StatusMessage = "Veriler işleniyor...";
            var monthlyDbList = await _uow.Hedefler.GetAllAylikHedeflerAsync();
            var weeklyDbList = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();
            
            // 3. Aylık ve Haftalık Kayıt (Sıralı)
            int count = 0;
            foreach(var item in currentPlans)
            {
                 count++;
                 StatusMessage = $"Kaydediliyor: {item.AyAdi} ({count}/12)...";

                 // Ay Kayıt
                 var mItem = monthlyDbList.FirstOrDefault(x => x.Yil == year && x.Ay == item.Ay) ?? new SatisHedefi { Yil = year, Ay = item.Ay };
                 mItem.HedefTutari = item.Hedef;
                  await _uow.Hedefler.SaveAylikHedefAsync(mItem);
                 
                 // Hafta Dağıtımı (Cache passed to avoid re-fetch but we modify objects in memory too)
                 await DistributeMonthlyToWeeklyInternal(year, item.Ay, item.Hedef, weeklyDbList);
            }
            
            // 4. Bitiş
            StatusMessage = "Tamamlandı, sayfa yenileniyor...";
            await Task.Delay(500); 

            IsEditMode = false;
            await LoadYearData(year); 
            
            if (SelectedAylikPlan != null) 
            {
                await LoadWeeklyData(SelectedAylikPlan.Ay);
            }
            StatusMessage = "Kayıt Başarılı!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"HATA: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SAVE ERROR: {ex}");
            // Log to console explicitly
            Console.WriteLine($"[SaveError] {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DistributeMonthlyToWeeklyInternal(int year, int month, decimal monthlyTarget, List<HaftalikSatisHedefi> allWeeklyTargets)
    {
        var weeksInMonth = GetWeeksInMonth(year, month);
        if (weeksInMonth.Count == 0) return;

        decimal targetPerWeek = Math.Round(monthlyTarget / weeksInMonth.Count, 2);

        foreach (var wk in weeksInMonth)
        {
             var existing = allWeeklyTargets.FirstOrDefault(x => x.Yil == year && x.Hafta == wk);
             if (existing == null) 
             {
                 existing = new HaftalikSatisHedefi { Yil = year, Hafta = wk };
                 allWeeklyTargets.Add(existing); 
             }
             
             existing.HedefTutari = targetPerWeek;
             await _uow.Hedefler.SaveHaftalikHedefAsync(existing);
        }
    }

    private List<int> GetWeeksInMonth(int year, int month)
    {
        var weeks = new List<int>();
        int totalWeeks = ISOWeek.GetWeeksInYear(year);
        for (int w = 1; w <= totalWeeks; w++)
        {
            try
            {
                // ISO Standard: The week belongs to the month/year that contains its Thursday
                var thursday = ISOWeek.ToDateTime(year, w, DayOfWeek.Thursday);
                
                // Note: We only care if the week falls in our target month.
                // Since this is View logic for "January Weeks", we follow the rule.
                if (thursday.Month == month) 
                {
                    weeks.Add(w);
                }
            } catch {}
        }
        return weeks;
    }
    
    private async Task DistributeMonthlyToWeekly(int year, int month, decimal monthlyTarget)
    {
        // 1. Find ISO weeks in this month
        var weeksInMonth = new List<int>();
        int totalWeeks = ISOWeek.GetWeeksInYear(year);
        
        for (int w = 1; w <= totalWeeks; w++)
        {
            try
            {
                var monday = ISOWeek.ToDateTime(year, w, DayOfWeek.Monday);
                if (monday.Month == month) weeksInMonth.Add(w);
            } catch {}
        }

        if (weeksInMonth.Count == 0 || monthlyTarget == 0) return;

        decimal targetPerWeek = monthlyTarget / weeksInMonth.Count;
        
        var allTargets = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();

        foreach (var wk in weeksInMonth)
        {
             var existing = allTargets.FirstOrDefault(x => x.Yil == year && x.Hafta == wk);
             // Verify if user already manually edited weeks? 
             // Requirement says: "I want it to automatic create".
             // We can overwrite or only write if 0?
             // Usually automatic distribution implies overwriting or filling initial. 
             // Let's overwite for now to ensure consistency, or maybe check if sum matches?
             // Simple logic: Overwrite.
             
             if (existing == null) existing = new HaftalikSatisHedefi { Yil = year, Hafta = wk };
             existing.HedefTutari = Math.Round(targetPerWeek, 2);
             
             await _uow.Hedefler.SaveHaftalikHedefAsync(existing);
        }
    }
    
    [RelayCommand]
    public async Task SaveHaftalikPlanlar()
    {
        // Save Weekly Grid Changes
        if (SelectedAylikPlan == null) return;
        
        foreach(var w in HaftalikPlanlar)
        {
             var allTargets = await _uow.Hedefler.GetAllHaftalikHedeflerAsync();
             var existing = allTargets.FirstOrDefault(x => x.Yil == SelectedYear && x.Hafta == w.Hafta);
             
             if (existing == null) existing = new HaftalikSatisHedefi { Yil = SelectedYear, Hafta = w.Hafta };
             existing.HedefTutari = w.Hedef;
             
             await _uow.Hedefler.SaveHaftalikHedefAsync(existing);
        }
        
        // Refresh
        await LoadWeeklyData(SelectedAylikPlan.Ay);
    }
}
