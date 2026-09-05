using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class TeklifListViewModel : ViewModelBase
{
    protected readonly IUnitOfWork _uow;
    protected readonly IPdfService _pdfService;
    protected readonly IFileService _fileService;
    
    [ObservableProperty]
    private ObservableCollection<Teklif> _teklifler = new();

    [ObservableProperty]
    private string _searchString = "";

    [ObservableProperty] private int _activeDateFilter = 30; // 0=Tümü, 7, 30, 90

    [ObservableProperty] private int _bekleyenTeklifCount;
    [ObservableProperty] private decimal _toplamTeklifTutari;

    partial void OnSearchStringChanged(string value) => _ = LoadTekliflerAsync();
    partial void OnActiveDateFilterChanged(int value) => _ = LoadTekliflerAsync();

    public TeklifListViewModel(IUnitOfWork uow, IPdfService pdfService, IFileService fileService)
    {
        _uow = uow;
        _pdfService = pdfService;
        _fileService = fileService;
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = LoadTekliflerAsync();
    }

    [ObservableProperty]
    private Teklif? _selectedTeklif;

    [RelayCommand]
    public async Task LoadTekliflerAsync()
    {
        IsLoading = true;
        try
        {
            var all = await _uow.Teklifler.GetAllAsync();
            
            // Stats
            BekleyenTeklifCount = all.Count(t => t.Durum == "Bekliyor");
            ToplamTeklifTutari = all.Sum(t => t.GenelToplam);

            var list = all;

            // Date Filter
            if (ActiveDateFilter > 0)
            {
                var cutoff = DateTime.Now.Date.AddDays(-ActiveDateFilter);
                list = list.Where(t => t.Tarih >= cutoff).ToList();
            }
            
            // Search
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
        catch (Exception ex)
        {
            ErrorMessage = $"Yükleme Hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event Action<Teklif?>? OnEditRequested;
    public event Action? OnCreateRequested;

    [RelayCommand]
    public void CreateNewQuote()
    {
        OnCreateRequested?.Invoke();
    }

    [RelayCommand]
    public void EditTeklif(Teklif teklif)
    {
        if (teklif == null) return;
        OnEditRequested?.Invoke(teklif);
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

    [RelayCommand]
    public async Task ViewTeklifPdfAsync(Teklif teklif)
    {
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
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ShareTeklifWhatsAppAsync(Teklif? teklif)
    {
        if (teklif == null) return;
        try
        {
            var details = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
            var cari = await _uow.Cariler.GetByIdAsync(teklif.CariId);
            var pdfBytes = await _pdfService.GenerateTeklifPdfBytesAsync(teklif, details, cari);

            if (pdfBytes == null || pdfBytes.Length == 0) return;

            string telefon = cari?.Telefon ?? cari?.CepTelefon ?? "";
            string mesaj = $"Sayın {cari?.Unvan}, {teklif.TeklifNo} numaralı teklif belgeniz ekte sunulmuştur.";
            string safeNo = string.Join("_", (teklif.TeklifNo ?? "NO").Split(System.IO.Path.GetInvalidFileNameChars()));
            string fileName = $"Teklif_{safeNo}.pdf";

            var shareService = ResolveShareService();
            if (shareService != null)
            {
                await shareService.ShareViaWhatsAppAsync(telefon, mesaj, pdfBytes, fileName);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"WhatsApp Paylaşım Hatası: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ShareTeklifEmailAsync(Teklif? teklif)
    {
        if (teklif == null) return;
        try
        {
            var details = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
            var cari = await _uow.Cariler.GetByIdAsync(teklif.CariId);
            
            if (cari == null || string.IsNullOrWhiteSpace(cari.Email))
            {
                ErrorMessage = "Cari karta tanımlı e-posta adresi bulunamadı.";
                return;
            }

            var pdfBytes = await _pdfService.GenerateTeklifPdfBytesAsync(teklif, details, cari);
            if (pdfBytes == null || pdfBytes.Length == 0) return;

            string aliciEposta = cari.Email;
            string konu = $"Teklif - {teklif.TeklifNo}";
            string mesaj = $"Sayın {cari.Unvan},\n\n{teklif.TeklifNo} numaralı teklif belgeniz ekte yer almaktadır.\n\nİyi çalışmalar.";
            string safeNo = string.Join("_", (teklif.TeklifNo ?? "NO").Split(System.IO.Path.GetInvalidFileNameChars()));
            string fileName = $"Teklif_{safeNo}.pdf";

            var shareService = ResolveShareService();
            if (shareService != null)
            {
                await shareService.SendPdfViaEmailAsync(aliciEposta, konu, mesaj, pdfBytes, fileName);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"E-posta Gönderim Hatası: {ex.Message}";
        }
    }

    private dynamic? ResolveShareService()
    {
        try
        {
            var appType = Type.GetType("Avalonia.Application, Avalonia.Base");
            if (appType == null) appType = Type.GetType("Avalonia.Application, Avalonia.Controls");
            if (appType == null) return null;

            var currentProp = appType.GetProperty("Current");
            var currentApp = currentProp?.GetValue(null);
            if (currentApp == null) return null;

            var servicesProp = currentApp.GetType().GetProperty("Services");
            var services = servicesProp?.GetValue(currentApp);
            if (services == null) return null;

            var getServiceMethod = services.GetType().GetMethod("GetService", new Type[] { typeof(Type) });
            if (getServiceMethod == null) return null;

            var shareServiceType = Type.GetType("ErmayMuhasebe.Avalonia.Services.ShareService, ErmayMuhasebe.Avalonia");
            if (shareServiceType != null)
            {
                return getServiceMethod.Invoke(services, new object[] { shareServiceType });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ResolveShareService Error: {ex.Message}");
        }
        return null;
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
    public async Task ConvertToOrderAsync()
    {
        if (SelectedTeklif == null) return;
        
        var details = await _uow.Teklifler.GetDetaylarAsync(SelectedTeklif.Id);
        
        var siparis = new Siparis
        {
            SiparisNo = "SIP-" + DateTime.Now.Ticks.ToString().Substring(12),
            CariId = SelectedTeklif.CariId,
            CariUnvan = SelectedTeklif.CariUnvan,
            Tarih = DateTime.Now,
            Aciklama = $"Teklif Ref: {SelectedTeklif.TeklifNo}. {SelectedTeklif.Aciklama}",
            Durum = "Bekliyor",
            Oncelik = "Normal",
            BaglantiEvrakNo = SelectedTeklif.TeklifNo
        };

        var siparisDetaylar = new List<SiparisDetay>();

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

        await _uow.Siparisler.SaveWithDetailsAsync(siparis, siparisDetaylar);

        SelectedTeklif.Durum = "Siparişleşti";
        await _uow.Teklifler.SaveAsync(SelectedTeklif);

        await LoadTekliflerAsync();
        SuccessMessage = "Teklif başarıyla siparişe dönüştürüldü.";
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
