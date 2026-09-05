using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class YearSelectionViewModel : ViewModelBase
{
    private readonly IYearContext _yearContext;

    [ObservableProperty] private ObservableCollection<int> _years = new();
    [ObservableProperty] private int? _selectedYear;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    public YearSelectionViewModel(IYearContext yearContext)
    {
        _yearContext = yearContext;
        LoadYears();
    }

    private void LoadYears()
    {
        // Simple default years for now, in real app this would call an API or scan DB
        Years = new ObservableCollection<int> { 2026, 2025, 2024, 2023 };
        SelectedYear = _yearContext.CurrentYear > 0 ? _yearContext.CurrentYear : DateTime.Today.Year;
    }

    [RelayCommand]
    public async Task SelectYearAsync()
    {
        if (SelectedYear == null) return;

        IsBusy = true;
        StatusMessage = $"{SelectedYear} yılı veritabanı yükleniyor...";
        
        try
        {
            await Task.Delay(800); // UI feel
            _yearContext.CurrentYear = SelectedYear.Value;
            StatusMessage = "Yıl seçimi başarılı.";
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
