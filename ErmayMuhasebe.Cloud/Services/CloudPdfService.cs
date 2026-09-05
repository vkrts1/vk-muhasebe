using System.Net.Http.Json;
using ErmayMuhasebe.Models;
using Microsoft.JSInterop;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.Services;

public class CloudPdfService : IPdfService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _js;
    private readonly IUnitOfWork _uow;
    
    // API URL - Google Cloud Run 7/24 Canlı PDF Servisi
    private const string ApiBaseUrl = "https://ermay-pdf-api-916435485627.europe-west1.run.app";
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() { MaxDepth = 256, PropertyNameCaseInsensitive = true };

    public CloudPdfService(HttpClient httpClient, IJSRuntime js, IUnitOfWork uow)
    {
        _httpClient = httpClient;
        _js = js;
        _uow = uow;
    }

    private async Task<(bool Success, string Error)> SendPdfRequest(string endpoint, object data)
    {
        try
        {
            Console.WriteLine($"PDF isteği gönderiliyor: {endpoint}");
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/generate/{endpoint}", data, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return (false, "Sunucu boş bir PDF dosyası döndürdü.");
                }

                Console.WriteLine($"PDF alındı: {pdfBytes.Length} byte. JS tarafına aktarılıyor...");
                await _js.InvokeVoidAsync("openPdf", pdfBytes);
                return (true, "");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Sunucu Hatası ({response.StatusCode}): {errorContent}");
        }
        catch (Exception ex)
        {
            var targetFullUrl = $"{ApiBaseUrl}/generate/{endpoint}";
            Console.WriteLine($"[PDF-CLIENT] HATA! URL: {targetFullUrl} | Mesaj: {ex.Message}");
            
            if (ex.Message.Contains("fetch", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Tarayıcı '{targetFullUrl}' adresine bağlanamadı (CORS veya Ağ Hatası).\n1. API penceresinin açık olduğunu kontrol edin.\n2. Web uygulamasını HTTPS (https://) ile değil, mutlaka HTTP (http://localhost:5242) üzerinden açtığınızdan emin olun.");
            }
            
        return (false, $"Bağlantı Hatası: {ex.Message}");
        }
    }

    private async Task<byte[]> FetchPdfBytes(string endpoint, object data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/generate/{endpoint}", data, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDF-FETCH] HATA! {endpoint} | {ex.Message}");
        }
        return Array.Empty<byte>();
    }

    private async Task<byte[]?> GetCurrentLogoBytesAsync()
    {
        try 
        {
            var profil = await _uow.GetFirmaProfiliAsync();
            if (profil != null && !string.IsNullOrEmpty(profil.LogoBase64))
            {
                return Convert.FromBase64String(profil.LogoBase64);
            }
        } 
        catch { }
        return new byte[0]; // Logo yoksa boş dizi gönder ki API varsayılanı göstermesin
    }

    public async Task<(bool Success, string Error)> GenerateFaturaPdfAsync(Fatura fatura, List<FaturaDetay> detaylar) 
    {
        var profil = await _uow.GetFirmaProfiliAsync();
        return await SendPdfRequest("fatura", new ErmayMuhasebe.Models.FaturaRequest(fatura, detaylar, await GetCurrentLogoBytesAsync(), profil?.LogoFatura ?? true, FaturaSize, FaturaOrientation, AktifFaturaTasarimi));
    }

    public async Task<(bool Success, string Error)> GenerateBatchFaturaPdfAsync(List<(Fatura Fatura, List<FaturaDetay> Detaylar)> items)
    {
        var profil = await _uow.GetFirmaProfiliAsync();
        var logo = await GetCurrentLogoBytesAsync();
        var requests = items.Select(x => new ErmayMuhasebe.Models.FaturaRequest(x.Fatura, x.Detaylar, logo, profil?.LogoFatura ?? true, FaturaSize, FaturaOrientation, AktifFaturaTasarimi)).ToList();
        return await SendPdfRequest("fatura-batch", requests);
    }
 
    public async Task<(bool Success, string Error)> GenerateTeklifPdfAsync(Teklif teklif, List<TeklifDetay> detaylar, CariKart? cari)
    {
        var profil = await _uow.GetFirmaProfiliAsync();
        return await SendPdfRequest("teklif", new ErmayMuhasebe.Models.TeklifRequest(teklif, detaylar, cari, await GetCurrentLogoBytesAsync(), profil?.LogoTeklif ?? true, TeklifSize, TeklifOrientation));
    }
 
    public async Task<(bool Success, string Error)> GenerateSiparisPdfAsync(Siparis siparis, List<SiparisDetay> detaylar, CariKart? cari = null)
    {
        var profil = await _uow.GetFirmaProfiliAsync();
        return await SendPdfRequest("siparis", new ErmayMuhasebe.Models.SiparisRequest(siparis, detaylar, cari, await GetCurrentLogoBytesAsync(), profil?.LogoSiparis ?? true, SiparisSize, SiparisOrientation));
    }

    public async Task<(bool Success, string Error)> GenerateGenericTablePdfAsync(string title, string[] headers, List<string[]> rows, string? subtitle = null)
        => await SendPdfRequest("generic", new { Title = title, Headers = headers, Rows = rows, Subtitle = subtitle });

    public async Task<(bool Success, string Error)> GenerateConsolidatedReportPdfAsync(string title, List<ReportSection> sections)
        => await SendPdfRequest("consolidated", new { Title = title, Sections = sections });

    public async Task<(bool Success, string Error)> GenerateBudgetReportPdfAsync(string title, List<ChartDataItem> annual, List<ChartDataItem> monthly, List<ChartDataItem> weekly)
        => await SendPdfRequest("budget", new { Title = title, Annual = annual, Monthly = monthly, Weekly = weekly });

    public async Task<(bool Success, string Error)> GenerateMakbuzPdfAsync(string? makbuzTipi, string? cariUnvan, DateTime tarih, decimal tutar, string? aciklama)
        => await SendPdfRequest("makbuz", new { MakbuzTipi = makbuzTipi, CariUnvan = cariUnvan, Tarih = tarih, Tutar = tutar, Aciklama = aciklama });

    public async Task<(bool Success, string Error)> GenerateCariEkstrePdfAsync(CariKart cari, List<CariHareket> hareketler)
        => await SendPdfRequest("ekstre", new { Cari = cari, Hareketler = hareketler });

    public async Task<(bool Success, string Error)> GenerateDetayliCariEkstrePdfAsync(CariKart cari, List<CariHareket> hareketler, Dictionary<int, List<FaturaDetay>> detaylar)
        => await SendPdfRequest("ekstre-detayli", new { Cari = cari, Hareketler = hareketler, DetayDictionary = detaylar });

    public async Task<(bool Success, string Error)> GenerateEftPdfAsync(EftIslem islem)
        => await SendPdfRequest("eft", new { Islem = islem });

    public async Task<(bool Success, string Error)> GenerateKkPdfAsync(KrediKartiIslem request)
        => await SendPdfRequest("kk", new { Islem = request });

    public async Task<byte[]> GenerateFaturaPdfBytesAsync(Fatura fatura, List<FaturaDetay> detaylar)
        => await FetchPdfBytes("fatura", new ErmayMuhasebe.Models.FaturaRequest(fatura, detaylar, LogoBytes, ShowLogoFatura, FaturaSize, FaturaOrientation, AktifFaturaTasarimi));

    public async Task<byte[]> GenerateTeklifPdfBytesAsync(Teklif teklif, List<TeklifDetay> detaylar, CariKart? cari)
        => await FetchPdfBytes("teklif", new ErmayMuhasebe.Models.TeklifRequest(teklif, detaylar, cari, LogoBytes, ShowLogoTeklif, TeklifSize, TeklifOrientation));

    public async Task<byte[]> GenerateSiparisPdfBytesAsync(Siparis siparis, List<SiparisDetay> detaylar, CariKart? cari)
        => await FetchPdfBytes("siparis", new ErmayMuhasebe.Models.SiparisRequest(siparis, detaylar, cari, LogoBytes, ShowLogoSiparis, SiparisSize, SiparisOrientation));

    public async Task<byte[]> GenerateGenericTablePdfBytesAsync(string title, string[] headers, List<string[]> rows, string? subtitle = null)
        => await FetchPdfBytes("generic", new { Title = title, Headers = headers, Rows = rows, Subtitle = subtitle });

    public async Task<byte[]> GenerateCariEkstrePdfBytesAsync(CariKart cari, List<CariHareket> hareketler)
        => await FetchPdfBytes("ekstre", new { Cari = cari, Hareketler = hareketler });

    public async Task<byte[]> GenerateDetayliCariEkstrePdfBytesAsync(CariKart cari, List<CariHareket> hareketler, Dictionary<int, List<FaturaDetay>> detaylar)
        => await FetchPdfBytes("ekstre-detayli", new { Cari = cari, Hareketler = hareketler, DetayDictionary = detaylar });

    public async Task<byte[]> GenerateFaturaBatchPdfBytesAsync(List<(Fatura Fatura, List<FaturaDetay> Detaylar)> items)
    {
        var requests = items.Select(x => new ErmayMuhasebe.Models.FaturaRequest(x.Fatura, x.Detaylar, LogoBytes, ShowLogoFatura)).ToList();
        return await FetchPdfBytes("fatura-batch", requests);
    }

    public async Task<byte[]> GenerateMakbuzPdfBytesAsync(string? makbuzTipi, string? cariUnvan, DateTime tarih, decimal tutar, string? aciklama)
        => await FetchPdfBytes("makbuz", new { MakbuzTipi = makbuzTipi, CariUnvan = cariUnvan, Tarih = tarih, Tutar = tutar, Aciklama = aciklama });

    public async Task<byte[]> GenerateEftSlipPdfBytesAsync(EftIslem islem)
        => await FetchPdfBytes("eft", new { Islem = islem });

    public async Task<byte[]> GenerateKrediKartiSlipPdfBytesAsync(KrediKartiIslem islem)
        => await FetchPdfBytes("kk", new { Islem = islem });

    public async Task<byte[]> GenerateBudgetReportPdfBytesAsync(string title, List<ChartDataItem> annual, List<ChartDataItem> monthly, List<ChartDataItem> weekly)
        => await FetchPdfBytes("budget", new { Title = title, Annual = annual, Monthly = monthly, Weekly = weekly });

    public async Task<byte[]> GenerateConsolidatedReportPdfBytesAsync(string title, List<ReportSection> sections)
        => await FetchPdfBytes("consolidated", new { Title = title, Sections = sections });

    public async Task<byte[]> GenerateKasaEkstrePdfBytesAsync(BankaKart kasa, List<KasaHareket> hareketler)
        => await FetchPdfBytes("kasa-ekstre", new { Account = kasa, Movements = hareketler });

    public async Task<byte[]> GenerateMakbuzFromKasaPdfBytesAsync(KasaHareket h)
        => await FetchPdfBytes("makbuz-from-kasa", new { Movement = h });

    public async Task<byte[]> GenerateCekPdfBytesAsync(Cek cek)
        => await FetchPdfBytes("cek", new { Cek = cek });

    public async Task<byte[]> GenerateCekListPdfBytesAsync(List<Cek> cekler)
        => await FetchPdfBytes("cek-list", new { Items = cekler });

    public async Task<byte[]> GenerateKrediKartiListPdfBytesAsync(List<KrediKartiIslem> islemler)
        => await FetchPdfBytes("kk-list", new { Items = islemler });

    public async Task<byte[]> GenerateEftListPdfBytesAsync(List<EftIslem> islemler)
        => await FetchPdfBytes("eft-list", new { Items = islemler });

    public async Task<byte[]> GenerateKasaListesiPdfBytesAsync(List<BankaKart> kasalar)
        => await FetchPdfBytes("kasa-list", new { Items = kasalar });

    public async Task<byte[]> GenerateAdresEtiketiPdfBytesAsync(string unvan, string adSoyad, string adres, string ilIlce, string postaKodu, string telefon)
        => await FetchPdfBytes("adres-etiketi", new { Unvan = unvan, AdSoyad = adSoyad, Adres = adres, IlIlce = ilIlce, PostaKodu = postaKodu, Telefon = telefon });

    public async Task<byte[]> GenerateFaturaListPdfBytesAsync(List<Fatura> faturalar, DateTime startDate, DateTime endDate)
        => await FetchPdfBytes("fatura-list", new { Items = faturalar, StartDate = startDate, EndDate = endDate });

    public async Task<byte[]> GenerateFinansRaporPdfBytesAsync(string title, FinancialReportData data)
        => await FetchPdfBytes("finans-rapor", new { Title = title, Data = data, LogoBytes = LogoBytes, ShowLogo = ShowLogoRaporlar });

    public async Task<byte[]> GenerateStokListPdfBytesAsync(List<StokKart> stoklar)
        => await FetchPdfBytes("stok-list", new { Items = stoklar });

    public async Task<byte[]> GenerateStokHareketleriPdfBytesAsync(StokKart stok, List<StokHareket> hareketler)
        => await FetchPdfBytes("stok-hareket", new { Stock = stok, Movements = hareketler, LogoBytes = LogoBytes, ShowLogo = ShowLogoRaporlar });

    public async Task<byte[]> GenerateVadeRaporuPdfBytesAsync(List<VadeReportItem> items)
        => await FetchPdfBytes("vade-raporu", new { Items = items });

    public async Task<(bool Success, string Error)> GenerateMusteriTakipRaporuPdfAsync(MusteriTakipKlasor klasor, List<MusteriTakipDetay> detaylar)
        => await SendPdfRequest("musteri-takip", new { Klasor = klasor, Detaylar = detaylar });

    public async Task<byte[]> GenerateMusteriTakipRaporuPdfBytesAsync(MusteriTakipKlasor klasor, List<MusteriTakipDetay> detaylar)
        => await FetchPdfBytes("musteri-takip", new { Klasor = klasor, Detaylar = detaylar });

    public byte[]? LogoBytes { get; set; }
    public bool ShowLogoFatura { get; set; } = true;
    public bool ShowLogoSiparis { get; set; } = true;
    public bool ShowLogoTeklif { get; set; } = true;
    public bool ShowLogoEkstre { get; set; } = true;
    public bool ShowLogoRaporlar { get; set; } = true;

    public string FaturaSize { get; set; } = "A4";
    public string FaturaOrientation { get; set; } = "Portrait";
    public ErmayMuhasebe.Models.FaturaTasarimi? AktifFaturaTasarimi { get; set; }
    public string TeklifSize { get; set; } = "A4";
    public string TeklifOrientation { get; set; } = "Portrait";
    public string SiparisSize { get; set; } = "A5";
    public string SiparisOrientation { get; set; } = "Portrait";
}
