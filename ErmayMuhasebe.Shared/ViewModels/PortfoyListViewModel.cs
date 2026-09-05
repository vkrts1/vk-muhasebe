using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using MiniExcelLibs;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class PortfoyListViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    private readonly IExcelService _excelService;
    private readonly IFileService _fileService;

    [ObservableProperty] private ObservableCollection<PortfoyKart> _portfoyListesi = new();
    [ObservableProperty] private PortfoyKart? _selectedPortfoy;
    [ObservableProperty] private bool _isAddDialogVisible;
    [ObservableProperty] private PortfoyKart _newPortfoy = new();
    [ObservableProperty] private string _bulkData = ""; 
    [ObservableProperty] private bool _isBulkDialogVisible;
    [ObservableProperty] private int _selectedTab = 0; // 0: Temel, 1: İletişim, 2: Risk

    public PortfoyListViewModel(IUnitOfWork uow, IExcelService excelService, IFileService fileService)
    {
        _uow = uow;
        _excelService = excelService;
        _fileService = fileService;
        _ = LoadPortfoyAsync();
    }

    [RelayCommand]
    public async Task LoadPortfoyAsync()
    {
        var list = await _uow.Portfolyo.GetAllAsync();
        PortfoyListesi = new ObservableCollection<PortfoyKart>(list);
    }

    [RelayCommand]
    public void OpenAddPortfoy()
    {
        NewPortfoy = new PortfoyKart 
        { 
            PortfoyNo = "P-" + DateTime.Now.Ticks.ToString().Substring(10),
            CariTipi = "Müşteri"
        };
        SelectedTab = 0;
        IsAddDialogVisible = true;
    }

    [RelayCommand]
    public async Task SavePortfoyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPortfoy.FirmaIsmi)) return;
        await _uow.Portfolyo.SaveAsync(NewPortfoy);
        IsAddDialogVisible = false;
        await LoadPortfoyAsync();
    }

    [RelayCommand]
    public async Task DownloadExcelTemplateAsync()
    {
        var template = new List<PortfoyKart> { new PortfoyKart { FirmaIsmi = "Örnek Firma Ltd Şti", GSM = "05xx", Sektor = "Teknoloji" } };
        var bytes = await _excelService.ExportListToMemoryAsync(template, "PortfoySablone");
        
        var path = await _fileService.SaveFilePickerAsync("Excel Kaydet", "Portfoy_Sablon.xlsx", "xlsx");
        if (!string.IsNullOrEmpty(path))
        {
            await System.IO.File.WriteAllBytesAsync(path, bytes);
        }
    }

    [RelayCommand]
    public async Task DownloadCariListAsync()
    {
        var cariler = await _uow.Cariler.GetAllAsync();
        var bytes = await _excelService.ExportListToMemoryAsync(cariler.ToList(), "Cariler");
        var path = await _fileService.SaveFilePickerAsync("Excel Kaydet", "Mevcut_Cariler.xlsx", "xlsx");
        if (!string.IsNullOrEmpty(path))
        {
            await System.IO.File.WriteAllBytesAsync(path, bytes);
        }
    }

    [RelayCommand]
    public async Task UploadExcelAsync()
    {
        var file = await _fileService.OpenFilePickerAsync("Excel Files", new[] { "xlsx" });
        if (file == null) return;

        using (var stream = await file.OpenReadAsync())
        {
            var rows = MiniExcel.Query(stream, useHeaderRow: true).ToList();
            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object>)row;
                var item = PortfoyKart.FromDictionary(dict);
                if (string.IsNullOrWhiteSpace(item.FirmaIsmi)) continue;
                if (string.IsNullOrEmpty(item.PortfoyNo)) item.PortfoyNo = "P-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                await _uow.Portfolyo.SaveAsync(item);
            }
        }
        await LoadPortfoyAsync();
    }

    [RelayCommand]
    public async Task TransferToCariAsync(PortfoyKart? portfoy)
    {
        if (portfoy == null) return;

        var cari = new CariKart
        {
            CariKod = portfoy.PortfoyNo?.StartsWith("C-") == true ? portfoy.PortfoyNo : "C-" + portfoy.PortfoyNo,
            Unvan = portfoy.FirmaIsmi,
            Grup = portfoy.CariTipi ?? "Müşteri",
            Tur = (portfoy.CariTipi == "Tedarikçi") ? "Satici" : "Alici",
            Yetkili = portfoy.YetkiliKisi,
            VadeGunu = portfoy.VadeGunu,
            RiskLimiti = portfoy.RiskLimiti,
            
            VergiNo = portfoy.VKN,
            VergiDairesi = portfoy.VergiDairesi,
            CepTelefon = portfoy.GSM,
            Telefon = portfoy.Telefon,
            Email = portfoy.Email,
            WebAdresi = portfoy.WebSitesi,
            
            Ulke = portfoy.Ulke,
            Il = portfoy.Il,
            Ilce = portfoy.Ilce,
            Adres = portfoy.Adres,
            SevkAdresi = portfoy.SevkAdresi,
            
            Aciklama = portfoy.Aciklama,
            Borc = portfoy.AcilisBakiyesi,
            
            RiskTakibiYapilsin = portfoy.RiskTakibiYapilsin,
            VadeGecmisteEngelle = portfoy.VadeGecmisteEngelle,
            FaturadaRiskKontrolu = portfoy.FaturadaRiskKontrolu
        };

        if (portfoy.AcilisBakiyesi < 0) {
            cari.Alacak = Math.Abs(portfoy.AcilisBakiyesi);
            cari.Borc = 0;
        }

        await _uow.Cariler.SaveAsync(cari);
        await _uow.Portfolyo.DeleteAsync(portfoy.Id);
        await LoadPortfoyAsync();
    }

    [RelayCommand]
    public void EditPortfoy(PortfoyKart? portfoy)
    {
        if (portfoy == null) return;
        NewPortfoy = portfoy;
        SelectedTab = 0;
        IsAddDialogVisible = true;
    }

    [RelayCommand]
    public void DeletePortfoyConfirm(PortfoyKart? portfoy)
    {
        if (portfoy == null) return;
        ShowConfirm("Kayıt Sil", $"'{portfoy.FirmaIsmi}' portföy kaydını silmek istediğinize emin misiniz?", async () => {
            await _uow.Portfolyo.DeleteAsync(portfoy.Id);
            await LoadPortfoyAsync();
        });
    }

    public async Task DeletePortfoyAsync(PortfoyKart? portfoy)
    {
        if (portfoy == null) return;
        await _uow.Portfolyo.DeleteAsync(portfoy.Id);
        await LoadPortfoyAsync();
    }

    [RelayCommand]
    public void CloseDialogs()
    {
        IsAddDialogVisible = false;
        IsBulkDialogVisible = false;
    }
}
