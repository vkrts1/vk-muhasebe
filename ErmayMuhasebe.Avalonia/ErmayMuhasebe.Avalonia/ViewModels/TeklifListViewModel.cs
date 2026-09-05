using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using CommunityToolkit.Mvvm.Messaging;
using ErmayMuhasebe.Avalonia.Messages;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class TeklifListViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    private readonly IPdfService _pdfService;
    private readonly IFileService _fileService;
    
    [ObservableProperty]
    private ObservableCollection<Teklif> _teklifler = new();

    [ObservableProperty]
    private string _searchString = "";

    partial void OnSearchStringChanged(string value) => _ = LoadTekliflerAsync();

    public TeklifListViewModel(IUnitOfWork uow, IPdfService pdfService, IFileService fileService)
    {
        _uow = uow;
        _pdfService = pdfService;
        _fileService = fileService;
        _ = LoadTekliflerAsync();
    }

    [ObservableProperty]
    private Teklif? _selectedTeklif;
    


    [RelayCommand]
    public async Task LoadTekliflerAsync()
    {
        var list = await _uow.Teklifler.GetAllAsync();
        
        if (!string.IsNullOrWhiteSpace(SearchString))
        {
            list = list.Where(t => 
                (t.TeklifNo != null && t.TeklifNo.Contains(SearchString, StringComparison.OrdinalIgnoreCase)) ||
                (t.CariUnvan != null && t.CariUnvan.Contains(SearchString, StringComparison.OrdinalIgnoreCase)) ||
                (t.Aciklama != null && t.Aciklama.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        Teklifler = new ObservableCollection<Teklif>(list.OrderByDescending(x => x.Tarih));
    }

    [RelayCommand]
    public void CreateNewQuote()
    {
        var vm = new TeklifDetayViewModel(_uow, null);
        vm.RequestClose += () => {
             _ = LoadTekliflerAsync();
             WeakReferenceMessenger.Default.Send(new NavigateViewModelMessage(this));
        };
        WeakReferenceMessenger.Default.Send(new NavigateViewModelMessage(vm));
    }

    [RelayCommand]
    public async Task EditTeklifAsync(Teklif teklif)
    {
        if (teklif == null) return;
        var details = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
        var vm = new TeklifDetayViewModel(_uow, null);
        await vm.LoadFromExistingAsync(teklif, details);
        
        vm.RequestClose += () => {
             _ = LoadTekliflerAsync();
             WeakReferenceMessenger.Default.Send(new NavigateViewModelMessage(this));
        };
        WeakReferenceMessenger.Default.Send(new NavigateViewModelMessage(vm));
    }

    [RelayCommand]
    public void DeleteTeklifConfirm(Teklif teklif)
    {
        if (teklif == null) return;
        ShowConfirm("Teklif Sil", $"{teklif.TeklifNo} nolu teklifi silmek istediğinize emin misiniz?", async () => {
            await _uow.Teklifler.DeleteAsync(teklif);
            await LoadTekliflerAsync();
        });
    }

    public async Task DeleteTeklifAsync(Teklif teklif)
    {
        if (teklif == null) return;
        
        await _uow.Teklifler.DeleteAsync(teklif);
        await LoadTekliflerAsync();
    }

    [RelayCommand]
    public async Task ViewTeklifPdfAsync(Teklif? teklif)
    {
        if (teklif == null) teklif = SelectedTeklif;
        if (teklif == null) return;
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            var details = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
            var cari = await _uow.Cariler.GetByIdAsync(teklif.CariId);
            var pdfBytes = await _pdfService.GenerateTeklifPdfBytesAsync(teklif, details, cari);
 
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                ErrorMessage = "PDF içeriği oluşturulamadı.";
                return;
            }

            string safeNo = string.Join("_", (teklif.TeklifNo ?? "NO").Split(System.IO.Path.GetInvalidFileNameChars()));
            string fileName = $"Teklif_{safeNo}.pdf";

            await _fileService.SaveAndOpenFileAsync(fileName, pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"PDF Görüntüleme Hatası: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"PDF Error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ShowConvertToOrderConfirm()
    {
        if (SelectedTeklif == null) return;
        if (SelectedTeklif.Durum == "Siparişleşti") return;

        ShowConfirm("Siparişe Dönüştür", $"{SelectedTeklif.TeklifNo} nolu teklif siparişe dönüştürülecek. Emin misiniz?", async () => {
            await ConvertToOrderAsync();
        });
    }

    [RelayCommand]
    public void CloseConfirm() => IsConfirmVisible = false;

    [RelayCommand]
    public async Task ConvertToOrderAsync()
    {
        if (SelectedTeklif == null) return;
        IsConfirmVisible = false;
        
        var details = await _uow.Teklifler.GetDetaylarAsync(SelectedTeklif.Id);
        
        var siparis = new Siparis
        {
            SiparisNo = "SIP-" + System.DateTime.Now.Ticks.ToString().Substring(12),
            CariId = SelectedTeklif.CariId,
            CariUnvan = SelectedTeklif.CariUnvan,
            Tarih = System.DateTime.Now,
            Aciklama = $"Teklif Ref: {SelectedTeklif.TeklifNo}. {SelectedTeklif.Aciklama}",
            Durum = "Bekliyor",
            Oncelik = "Normal",
            BaglantiEvrakNo = SelectedTeklif.TeklifNo
        };

        var siparisDetaylar = new System.Collections.Generic.List<SiparisDetay>();

        decimal toplamTutar = 0;
        decimal toplamKdv = 0;

        foreach (var d in details)
        {
            var stok = await _uow.Stoklar.GetByIdAsync(d.StokId);
            var kdvOrani = (decimal)(d.KdvOrani > 0 ? d.KdvOrani : (stok?.KDV ?? 20));

            var sd = new SiparisDetay
            {
                StokId = d.StokId,
                StokAdi = d.StokAdi,
                Miktar = d.Miktar,
                TopMiktari = d.TopMiktari,
                Birim = d.Birim ?? stok?.Birim ?? "Adet",
                BirimFiyat = d.BirimFiyat,
                Tutar = d.Tutar,
                Aciklama = d.Aciklama,
                KdvOrani = (double)kdvOrani,
                ParaBirimi = d.ParaBirimi
            };
            siparisDetaylar.Add(sd);

            toplamTutar += d.Tutar;
            toplamKdv += d.Tutar * (kdvOrani / 100.0m);
        }

        siparis.GenelToplam = toplamTutar + toplamKdv;

        // Save Order
        await _uow.Siparisler.SaveWithDetailsAsync(siparis, siparisDetaylar);

        // Update Quote Status
        SelectedTeklif.Durum = "Siparişleşti";
        await _uow.Teklifler.SaveAsync(SelectedTeklif); // Update header only; do not pass empty list to SaveWithDetailsAsync which deletes details

        await LoadTekliflerAsync();
    }

    [RelayCommand]
    public async Task UpdateStatusAsync(string status)
    {
        if (SelectedTeklif == null || string.IsNullOrEmpty(status)) return;
        SelectedTeklif.Durum = status;
        await _uow.Teklifler.SaveAsync(SelectedTeklif);
        await LoadTekliflerAsync();
    }
}
