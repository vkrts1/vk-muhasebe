using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Cloud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using System;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class CariListViewModel : ErmayMuhasebe.Shared.ViewModels.CariListViewModel
{
    private readonly ErmayMuhasebe.Cloud.Services.IFileService _fileService;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    public CariListViewModel(ErmayMuhasebe.Repositories.IUnitOfWork uow, IPdfService pdfService, IExcelService excelService, ErmayMuhasebe.Cloud.Services.IFileService fileService, NavigationManager navigationManager, IJSRuntime jsRuntime, CekSenetListViewModel checkHelper, ExternalApiService externalApi, IFinansService finansService) 
        : base(uow, pdfService, excelService, checkHelper, externalApi, finansService)
    {
        _fileService = fileService;
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    protected override void NotifyFinancialDataChanged()
    {
        // No specific message needed for now
    }

    protected override async Task HandleFileOpenAsync(byte[] content, string fileName)
    {
        if (fileName.EndsWith(".pdf")) await _fileService.OpenPdfAsync(content);
        else 
        {
            string contentType = fileName.EndsWith(".xlsx") ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/octet-stream";
            await _fileService.DownloadFileAsync(fileName, contentType, content);
        }
    }

    public override void OpenSatisFaturasi()
    {
        if (SelectedCari == null) return;
        _navigationManager.NavigateTo($"/fatura-olustur/Satis/{SelectedCari.Id}");
    }

    public override void OpenAlisFaturasi()
    {
        if (SelectedCari == null) return;
        _navigationManager.NavigateTo($"/fatura-olustur/Alis/{SelectedCari.Id}");
    }

    public async Task DownloadPendingPdf()
    {
        if (_pendingPdfBytes != null)
        {
            await HandleFileOpenAsync(_pendingPdfBytes, _pendingPdfName);
            IsEkstreOptionVisible = false;
        }
    }

    public override async Task DownloadEkstreAsync(object? parameter)
    {
        if (_pendingPdfBytes == null) return;
        try
        {
            var fileName = string.IsNullOrWhiteSpace(_pendingPdfName)
                ? $"Ekstre_{SelectedCari?.Unvan}_{DateTime.Now:ddMMyyyy}.pdf"
                : _pendingPdfName;
            await _fileService.DownloadFileAsync(fileName, "application/pdf", _pendingPdfBytes);
            IsEkstreOptionVisible = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"İndirme hatası: {ex.Message}";
        }
    }

    public override async Task EditTransactionAsync()
    {
        if (SelectedHareket == null) return;
        
        if (SelectedHareket.IslemTuru != null && SelectedHareket.IslemTuru.Contains("Fatura"))
        {
             _navigationManager.NavigateTo($"/fatura-detay/{SelectedHareket.FaturaId ?? 0}");
        }
        else
        {
            // For now, only fatura editing is navigated. 
            // Others use the dialog within the ViewModel.
        }
        await Task.CompletedTask;
    }

    public override async Task ViewTransactionAsync()
    {
        if (SelectedHareket == null) return;
        StatusMessage = "Belge hazırlanıyor (WASM Sync Mode)...";
        ErrorMessage = "";

        try
        {
            string islemTuru = SelectedHareket.IslemTuru ?? "";

            if (islemTuru.Contains("Fatura", StringComparison.OrdinalIgnoreCase) || 
                islemTuru.Equals("Satış", StringComparison.OrdinalIgnoreCase) || 
                islemTuru.Equals("Alış", StringComparison.OrdinalIgnoreCase))
            {
                Fatura? fatura = null;
                if (SelectedHareket.FaturaId.HasValue && SelectedHareket.FaturaId.Value > 0)
                    fatura = await _uow.Faturalar.GetByIdAsync(SelectedHareket.FaturaId.Value);
                
                if (fatura == null && !string.IsNullOrEmpty(SelectedHareket.EvrakNo))
                    fatura = await _uow.Faturalar.GetByNoAsync(SelectedHareket.EvrakNo);

                if (fatura != null)
                {
                    var detaylar = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
                    StatusMessage = "Belge hazırlanıyor (Cloud PDF)...";
                    var (success, error) = await _pdfService.GenerateFaturaPdfAsync(fatura, detaylar);
                    
                    if (!success) ErrorMessage = $"PDF sunucusu hata döndürdü: {error}. Lütfen terminali kontrol edin.";
                    StatusMessage = "";
                    return;
                }
            }
            
            StatusMessage = "Belge hazırlanıyor...";
            bool serverSuccess = false;
            
            if (islemTuru.Contains("Havale", StringComparison.OrdinalIgnoreCase) || islemTuru.Contains("EFT", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _uow.EftIslemleri.GetByNoAsync(SelectedHareket.EvrakNo ?? "");
                if (result != null)
                {
                    var (s, e) = await _pdfService.GenerateEftPdfAsync(result);
                    serverSuccess = s;
                }
            }
            else if (islemTuru.Contains("Kart", StringComparison.OrdinalIgnoreCase) || islemTuru.Contains("Kredi", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _uow.KrediKartlari.GetByNoAsync(SelectedHareket.EvrakNo ?? "");
                if (result != null)
                {
                    var (s, e) = await _pdfService.GenerateKkPdfAsync(result);
                    serverSuccess = s;
                }
            }
            
            if (!serverSuccess)
            {
                decimal tutar = SelectedHareket.Borc > 0 ? SelectedHareket.Borc : SelectedHareket.Alacak;
                var (s, e) = await _pdfService.GenerateMakbuzPdfAsync(islemTuru, SelectedHareket.CariUnvan, SelectedHareket.Tarih, tutar, SelectedHareket.Aciklama);
                serverSuccess = s;
            }

            if (!serverSuccess)
            {
                // Final JS Fallback if Cloud API is down
                decimal tutar = SelectedHareket.Borc > 0 ? SelectedHareket.Borc : SelectedHareket.Alacak;
                await _jsRuntime.InvokeVoidAsync("generateReceiptPdfJs", 
                    SelectedHareket.CariUnvan ?? "-", 
                    islemTuru, 
                    tutar, 
                    SelectedHareket.Tarih.ToString("dd.MM.yyyy"), 
                    SelectedHareket.Aciklama ?? "-");
            }
            
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Hata: {ex.Message}";
            StatusMessage = "";
        }
    }
    public override async Task HesapEkstresiAsync(CariKart? cari = null)
    {
        if (cari != null) SelectedCari = cari;
        if (SelectedCari == null) return;
        try 
        {
            StatusMessage = "Hesap ekstresi hazırlanıyor...";
            var hareketler = await _uow.Cariler.GetHareketlerAsync(SelectedCari.Id);
            var (success, error) = await _pdfService.GenerateCariEkstrePdfAsync(SelectedCari, hareketler);
            if (!success) ErrorMessage = $"Hesap ekstresi hatası: {error}";
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Rapor hatası: {ex.Message}";
            StatusMessage = "";
        }
    }

    public override async Task DetayliHesapEkstresiAsync(CariKart? cari = null)
    {
        if (cari != null) SelectedCari = cari;
        if (SelectedCari == null) return;
        try 
        {
            IsLoading = true;
            StatusMessage = "Detaylı ekstre hazırlanıyor, bu işlem fatura sayısına göre zaman alabilir...";
            var hareketler = (await _uow.Cariler.GetHareketlerAsync(SelectedCari.Id)).OrderBy(h => h.Tarih).ToList();
            var detayDictionary = new Dictionary<int, List<FaturaDetay>>();

            foreach (var h in hareketler)
            {
                if (!string.IsNullOrEmpty(h.IslemTuru) && h.IslemTuru.Contains("Fatura", StringComparison.OrdinalIgnoreCase))
                {
                    int? targetFaturaId = h.FaturaId;
                    if (!targetFaturaId.HasValue && !string.IsNullOrEmpty(h.EvrakNo))
                    {
                         var fatura = await _uow.Faturalar.GetByNoAsync(h.EvrakNo);
                         if (fatura != null) targetFaturaId = fatura.Id;
                    }

                    if (targetFaturaId.HasValue && !detayDictionary.ContainsKey(targetFaturaId.Value))
                    {
                         var details = await _uow.Faturalar.GetDetaylarAsync(targetFaturaId.Value);
                         if(details != null) detayDictionary.Add(targetFaturaId.Value, details);
                    }
                }
            }

            var (success, error) = await _pdfService.GenerateDetayliCariEkstrePdfAsync(SelectedCari, hareketler, detayDictionary);
            if (!success) ErrorMessage = $"Detaylı ekstre hatası: {error}";
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Rapor hatası: {ex.Message}";
            StatusMessage = "";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override async Task CariYaslandirmaAsync(CariKart? cari = null)
    {
        if (cari != null) SelectedCari = cari;
        if (SelectedCari == null) return;
        try 
        {
            IsLoading = true;
            StatusMessage = "Yaşlandırma raporu hazırlanıyor...";
            var hareketler = await _uow.Cariler.GetHareketlerAsync(SelectedCari.Id);
            
            string title = $"{SelectedCari.Unvan} - Yaşlandırma Raporu";
            string[] headers = new[] { "Vade Tarihi", "Açıklama", "0-30 Gün", "31-60 Gün", "61-90 Gün", "90+ Gün", "Borç Bakiyesi" };
            List<string[]> rows = new();

            var borcHareketler = hareketler.Where(h => (h.Borc - h.Alacak) > 0).ToList();
            var now = DateTime.Now;

            foreach (var h in borcHareketler.OrderBy(h => h.Tarih))
            {
                decimal bakiye = h.Borc - h.Alacak;
                var days = (now - h.Tarih).TotalDays;
                
                string d0_30 = days <= 30 ? bakiye.ToString("C2") : "-";
                string d31_60 = (days > 30 && days <= 60) ? bakiye.ToString("C2") : "-";
                string d61_90 = (days > 60 && days <= 90) ? bakiye.ToString("C2") : "-";
                string d90plus = days > 90 ? bakiye.ToString("C2") : "-";

                rows.Add(new[] { 
                    h.Tarih.ToString("dd.MM.yyyy"), 
                    h.Aciklama ?? "-", 
                    d0_30, d31_60, d61_90, d90plus, 
                    bakiye.ToString("C2") 
                });
            }

            // Summary row
            decimal t0_30 = borcHareketler.Where(h => (now - h.Tarih).TotalDays <= 30).Sum(h => h.Borc - h.Alacak);
            decimal t31_60 = borcHareketler.Where(h => (now - h.Tarih).TotalDays > 30 && (now - h.Tarih).TotalDays <= 60).Sum(h => h.Borc - h.Alacak);
            decimal t61_90 = borcHareketler.Where(h => (now - h.Tarih).TotalDays > 60 && (now - h.Tarih).TotalDays <= 90).Sum(h => h.Borc - h.Alacak);
            decimal t90plus = borcHareketler.Where(h => (now - h.Tarih).TotalDays > 90).Sum(h => h.Borc - h.Alacak);
            
            rows.Add(new[] { "TOPLAM", "", t0_30.ToString("C2"), t31_60.ToString("C2"), t61_90.ToString("C2"), t90plus.ToString("C2"), (t0_30+t31_60+t61_90+t90plus).ToString("C2") });

            var (success, error) = await _pdfService.GenerateGenericTablePdfAsync(title, headers, rows);
            if (!success) ErrorMessage = $"Yaşlandırma hatası: {error}";
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Yaşlandırma hatası: {ex.Message}";
            StatusMessage = "";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
