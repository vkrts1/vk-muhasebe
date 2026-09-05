using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories.DataProviders;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class YearSelectionViewModel : ViewModelBase
{
    private readonly IYearContext _yearContext;
    private readonly IDataProvider _dataProvider;
    private readonly DatabaseService _dbService;
    private readonly Action<int> _onYearSelected;

    [ObservableProperty]
    private ObservableCollection<int> _years = new();

    [ObservableProperty]
    private int? _selectedYear;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "";

    public YearSelectionViewModel(
        IYearContext yearContext, 
        IDataProvider dataProvider, 
        DatabaseService dbService,
        Action<int> onYearSelected)
    {
        _yearContext = yearContext;
        _dataProvider = dataProvider;
        _dbService = dbService;
        _onYearSelected = onYearSelected;
        
        LoadYears();
    }

    private void LoadYears()
    {
        Years.Clear();
        
        // Correct path to match DatabaseService default or expected folder
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe");
        
        if (!Directory.Exists(appData))
        {
            Directory.CreateDirectory(appData);
        }

        var files = Directory.GetFiles(appData, "ermay_*.db");
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            
            // Filter files based on CurrentTenantId
            if (_dbService.CurrentTenantId == "default")
            {
                if (fileName.Contains("tenant_"))
                    continue;
            }
            else
            {
                if (!fileName.Contains($"_{_dbService.CurrentTenantId}"))
                    continue;
            }

            var parts = fileName.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int year))
            {
                if (!Years.Contains(year))
                {
                    Years.Add(year);
                }
            }
        }

        // If no years found, add current year as option
        if (!Years.Any())
        {
            Years.Add(DateTime.Now.Year);
        }

        var sortedYears = Years.OrderByDescending(y => y).ToList();
        Years = new ObservableCollection<int>(sortedYears);
        SelectedYear = Years.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SelectYearAsync()
    {
        if (SelectedYear == null) return;

        IsBusy = true;
        StatusMessage = $"{SelectedYear} yılı veritabanı yükleniyor...";
        
        try
        {
            _yearContext.CurrentYear = SelectedYear.Value;
            var dbName = $"ermay_{SelectedYear.Value}.db";
            await _dataProvider.InitializeAsync(dbName);
            
            _onYearSelected?.Invoke(SelectedYear.Value);
        }
        catch (Exception ex)
        {
            StatusMessage = "Hata: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
