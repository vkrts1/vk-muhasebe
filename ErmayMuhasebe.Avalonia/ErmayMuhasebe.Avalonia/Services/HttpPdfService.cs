using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Services
{
    // PDF üretimini tek altyapı üzerinden Functions PDF API sunucusuna (port 5244) yönlendiren
    // HTTP istemcisi. Sunucu erişilemezse hata yerine QuestPDF yerel üretimine (base) düşer.
    public class HttpPdfService : PdfService
    {
        private readonly HttpClient _http;
        private readonly IFileService? _fileService;

        public HttpPdfService(HttpClient httpClient, IFileService? fileService = null)
            : base(fileService)
        {
            _http = httpClient;
            _fileService = fileService;
            if (_http.BaseAddress == null)
                _http.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ERMAY_PDF_API") ?? "https://ermay-pdf-api-916435485627.europe-west1.run.app");
        }

        private async Task<byte[]?> PostPdfAsync<T>(string path, T body)
        {
            try
            {
                using var resp = await _http.PostAsJsonAsync(path, body);
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }

        private async Task<(bool Success, string Error)> SaveAndOpenAsync(string fileName, byte[] bytes)
        {
            if (_fileService == null) return (false, "FileService not available.");
            try
            {
                await _fileService.SaveAndOpenFileAsync(fileName, bytes, "application/pdf");
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public override async Task<byte[]> GenerateFaturaPdfBytesAsync(Fatura fatura, List<FaturaDetay> detaylar)
            => await PostPdfAsync("/generate/fatura", new FaturaRequest(fatura, detaylar, LogoBytes, ShowLogoFatura, FaturaSize, FaturaOrientation, AktifFaturaTasarimi))
                ?? await base.GenerateFaturaPdfBytesAsync(fatura, detaylar);

        public override async Task<byte[]> GenerateTeklifPdfBytesAsync(Teklif teklif, List<TeklifDetay> detaylar, CariKart? cari)
            => await PostPdfAsync("/generate/teklif", new TeklifRequest(teklif, detaylar, cari, LogoBytes, ShowLogoTeklif, TeklifSize, TeklifOrientation))
                ?? await base.GenerateTeklifPdfBytesAsync(teklif, detaylar, cari);

        public override async Task<byte[]> GenerateSiparisPdfBytesAsync(Siparis siparis, List<SiparisDetay> detaylar, CariKart? cari)
            => await PostPdfAsync("/generate/siparis", new SiparisRequest(siparis, detaylar, cari, LogoBytes, ShowLogoSiparis, SiparisSize, SiparisOrientation))
                ?? await base.GenerateSiparisPdfBytesAsync(siparis, detaylar, cari);

        public override async Task<byte[]> GenerateGenericTablePdfBytesAsync(string title, string[] headers, List<string[]> rows, string? subtitle = null)
            => await PostPdfAsync("/generate/generic", new GenericTableRequest(title, headers, rows, subtitle))
                ?? await base.GenerateGenericTablePdfBytesAsync(title, headers, rows, subtitle);

        public override async Task<byte[]> GenerateCariEkstrePdfBytesAsync(CariKart cari, List<CariHareket> hareketler)
            => await PostPdfAsync("/generate/ekstre", new EkstreRequest(cari, hareketler))
                ?? await base.GenerateCariEkstrePdfBytesAsync(cari, hareketler);

        public override async Task<byte[]> GenerateDetayliCariEkstrePdfBytesAsync(CariKart cari, List<CariHareket> hareketler, Dictionary<int, List<FaturaDetay>> detaylar)
            => await PostPdfAsync("/generate/ekstre-detayli", new DetayliEkstreRequest(cari, hareketler, detaylar))
                ?? await base.GenerateDetayliCariEkstrePdfBytesAsync(cari, hareketler, detaylar);

        public override async Task<byte[]> GenerateFaturaBatchPdfBytesAsync(List<(Fatura Fatura, List<FaturaDetay> Detaylar)> items)
            => await PostPdfAsync("/generate/fatura-batch", items.Select(i => new FaturaRequest(i.Fatura, i.Detaylar, LogoBytes, ShowLogoFatura, FaturaSize, FaturaOrientation, AktifFaturaTasarimi)).ToList())
                ?? await base.GenerateFaturaBatchPdfBytesAsync(items);

        public override async Task<byte[]> GenerateMakbuzPdfBytesAsync(string? makbuzTipi, string? cariUnvan, DateTime tarih, decimal tutar, string? aciklama)
            => await PostPdfAsync("/generate/makbuz", new MakbuzRequest(makbuzTipi, cariUnvan, tarih, tutar, aciklama))
                ?? await base.GenerateMakbuzPdfBytesAsync(makbuzTipi, cariUnvan, tarih, tutar, aciklama);

        public override async Task<byte[]> GenerateEftSlipPdfBytesAsync(EftIslem islem)
            => await PostPdfAsync("/generate/eft", new EftRequest(islem))
                ?? await base.GenerateEftSlipPdfBytesAsync(islem);

        public override async Task<byte[]> GenerateKrediKartiSlipPdfBytesAsync(KrediKartiIslem islem)
            => await PostPdfAsync("/generate/kk", new KkRequest(islem))
                ?? await base.GenerateKrediKartiSlipPdfBytesAsync(islem);

        public override async Task<byte[]> GenerateBudgetReportPdfBytesAsync(string title, List<ChartDataItem> annual, List<ChartDataItem> monthly, List<ChartDataItem> weekly)
            => await PostPdfAsync("/generate/budget", new BudgetReportRequest(title, annual, monthly, weekly))
                ?? await base.GenerateBudgetReportPdfBytesAsync(title, annual, monthly, weekly);

        public override async Task<byte[]> GenerateConsolidatedReportPdfBytesAsync(string title, List<ReportSection> sections)
            => await PostPdfAsync("/generate/consolidated", new ConsolidatedReportRequest(title, sections))
                ?? await base.GenerateConsolidatedReportPdfBytesAsync(title, sections);

        public override async Task<byte[]> GenerateKasaEkstrePdfBytesAsync(BankaKart kasa, List<KasaHareket> hareketler)
            => await PostPdfAsync("/generate/kasa-ekstre", new KasaEkstreRequest(kasa, hareketler))
                ?? await base.GenerateKasaEkstrePdfBytesAsync(kasa, hareketler);

        public override async Task<byte[]> GenerateMakbuzFromKasaPdfBytesAsync(KasaHareket h)
            => await PostPdfAsync("/generate/makbuz-from-kasa", new KasaMakbuzRequest(h))
                ?? await base.GenerateMakbuzFromKasaPdfBytesAsync(h);

        public override async Task<byte[]> GenerateCekPdfBytesAsync(Cek cek)
            => await PostPdfAsync("/generate/cek", new CekRequest(cek))
                ?? await base.GenerateCekPdfBytesAsync(cek);

        public override async Task<byte[]> GenerateCekListPdfBytesAsync(List<Cek> cekler)
            => await PostPdfAsync("/generate/cek-list", new CekListRequest(cekler))
                ?? await base.GenerateCekListPdfBytesAsync(cekler);

        public override async Task<byte[]> GenerateKrediKartiListPdfBytesAsync(List<KrediKartiIslem> islemler)
            => await PostPdfAsync("/generate/kk-list", new KrediKartiListRequest(islemler))
                ?? await base.GenerateKrediKartiListPdfBytesAsync(islemler);

        public override async Task<byte[]> GenerateEftListPdfBytesAsync(List<EftIslem> islemler)
            => await PostPdfAsync("/generate/eft-list", new EftListRequest(islemler))
                ?? await base.GenerateEftListPdfBytesAsync(islemler);

        public override async Task<byte[]> GenerateKasaListesiPdfBytesAsync(List<BankaKart> kasalar)
            => await PostPdfAsync("/generate/kasa-list", new KasaListRequest(kasalar))
                ?? await base.GenerateKasaListesiPdfBytesAsync(kasalar);

        public override async Task<byte[]> GenerateAdresEtiketiPdfBytesAsync(string unvan, string adSoyad, string adres, string ilIlce, string postaKodu, string telefon)
            => await PostPdfAsync("/generate/adres-etiketi", new AdresEtiketiRequest(unvan, adSoyad, adres, ilIlce, postaKodu, telefon))
                ?? await base.GenerateAdresEtiketiPdfBytesAsync(unvan, adSoyad, adres, ilIlce, postaKodu, telefon);

        public override async Task<byte[]> GenerateFaturaListPdfBytesAsync(List<Fatura> faturalar, DateTime startDate, DateTime endDate)
            => await PostPdfAsync("/generate/fatura-list", new FaturaListRequest(faturalar, startDate, endDate, LogoBytes, ShowLogoRaporlar))
                ?? await base.GenerateFaturaListPdfBytesAsync(faturalar, startDate, endDate);

        public override async Task<byte[]> GenerateFinansRaporPdfBytesAsync(string title, FinancialReportData data)
            => await PostPdfAsync("/generate/finans-rapor", new FinansRaporRequest(title, data, LogoBytes, ShowLogoRaporlar))
                ?? await base.GenerateFinansRaporPdfBytesAsync(title, data);

        public override async Task<byte[]> GenerateStokListPdfBytesAsync(List<StokKart> stoklar)
            => await PostPdfAsync("/generate/stok-list", new StokListReportRequest(stoklar, LogoBytes, ShowLogoRaporlar))
                ?? await base.GenerateStokListPdfBytesAsync(stoklar);

        public override async Task<byte[]> GenerateStokHareketleriPdfBytesAsync(StokKart stok, List<StokHareket> hareketler)
            => await PostPdfAsync("/generate/stok-hareket", new StokHareketReportRequest(stok, hareketler, LogoBytes, ShowLogoRaporlar))
                ?? await base.GenerateStokHareketleriPdfBytesAsync(stok, hareketler);

        public override async Task<byte[]> GenerateVadeRaporuPdfBytesAsync(List<VadeReportItem> items)
            => await PostPdfAsync("/generate/vade-rapor", new VadeRaporuRequest(items))
                ?? await base.GenerateVadeRaporuPdfBytesAsync(items);

        public override Task<(bool Success, string Error)> GenerateFaturaPdfAsync(Fatura fatura, List<FaturaDetay> detaylar)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/fatura", new FaturaRequest(fatura, detaylar, LogoBytes, ShowLogoFatura, FaturaSize, FaturaOrientation, AktifFaturaTasarimi)),
                () => base.GenerateFaturaPdfAsync(fatura, detaylar),
                $"Fatura_{fatura.FaturaNo}.pdf");

        public override Task<(bool Success, string Error)> GenerateTeklifPdfAsync(Teklif teklif, List<TeklifDetay> detaylar, CariKart? cari)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/teklif", new TeklifRequest(teklif, detaylar, cari, LogoBytes, ShowLogoTeklif, TeklifSize, TeklifOrientation)),
                () => base.GenerateTeklifPdfAsync(teklif, detaylar, cari),
                $"Teklif_{teklif.TeklifNo}.pdf");

        public override Task<(bool Success, string Error)> GenerateSiparisPdfAsync(Siparis siparis, List<SiparisDetay> detaylar, CariKart? cari = null)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/siparis", new SiparisRequest(siparis, detaylar, cari, LogoBytes, ShowLogoSiparis, SiparisSize, SiparisOrientation)),
                () => base.GenerateSiparisPdfAsync(siparis, detaylar, cari),
                $"Siparis_{siparis.SiparisNo}.pdf");

        public override Task<(bool Success, string Error)> GenerateGenericTablePdfAsync(string title, string[] headers, List<string[]> rows, string? subtitle = null)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/generic", new GenericTableRequest(title, headers, rows, subtitle)),
                () => base.GenerateGenericTablePdfAsync(title, headers, rows, subtitle),
                $"{title}.pdf");

        public override async Task<(bool Success, string Error)> GenerateBatchFaturaPdfAsync(List<(Fatura Fatura, List<FaturaDetay> Detaylar)> items)
        {
            if (!items.Any()) return (true, "");
            return await RunWithFallbackAsync(
                () => PostPdfAsync("/generate/fatura-batch", items.Select(i => new FaturaRequest(i.Fatura, i.Detaylar, LogoBytes, ShowLogoFatura, FaturaSize, FaturaOrientation, AktifFaturaTasarimi)).ToList()),
                () => base.GenerateBatchFaturaPdfAsync(items),
                $"Toplu_Faturalar_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public override Task<(bool Success, string Error)> GenerateMakbuzPdfAsync(string? makbuzTipi, string? cariUnvan, DateTime tarih, decimal tutar, string? aciklama)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/makbuz", new MakbuzRequest(makbuzTipi, cariUnvan, tarih, tutar, aciklama)),
                () => base.GenerateMakbuzPdfAsync(makbuzTipi, cariUnvan, tarih, tutar, aciklama),
                "Makbuz.pdf");

        public override Task<(bool Success, string Error)> GenerateEftPdfAsync(EftIslem islem)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/eft", new EftRequest(islem)),
                () => base.GenerateEftPdfAsync(islem),
                $"EFT_{islem.DekontNo}.pdf");

        public override Task<(bool Success, string Error)> GenerateKkPdfAsync(KrediKartiIslem islem)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/kk", new KkRequest(islem)),
                () => base.GenerateKkPdfAsync(islem),
                "KrediKarti_Slip.pdf");

        public override Task<(bool Success, string Error)> GenerateBudgetReportPdfAsync(string title, List<ChartDataItem> annual, List<ChartDataItem> monthly, List<ChartDataItem> weekly)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/budget", new BudgetReportRequest(title, annual, monthly, weekly)),
                () => base.GenerateBudgetReportPdfAsync(title, annual, monthly, weekly),
                "Butce_Raporu.pdf");

        public override Task<(bool Success, string Error)> GenerateConsolidatedReportPdfAsync(string title, List<ReportSection> sections)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/consolidated", new ConsolidatedReportRequest(title, sections)),
                () => base.GenerateConsolidatedReportPdfAsync(title, sections),
                $"{title}.pdf");

        public override Task<(bool Success, string Error)> GenerateCariEkstrePdfAsync(CariKart cari, List<CariHareket> hareketler)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/ekstre", new EkstreRequest(cari, hareketler)),
                () => base.GenerateCariEkstrePdfAsync(cari, hareketler),
                $"Ekstre_{cari.Unvan}.pdf");

        public override Task<(bool Success, string Error)> GenerateDetayliCariEkstrePdfAsync(CariKart cari, List<CariHareket> hareketler, Dictionary<int, List<FaturaDetay>> detaylar)
            => RunWithFallbackAsync(
                () => PostPdfAsync("/generate/ekstre-detayli", new DetayliEkstreRequest(cari, hareketler, detaylar)),
                () => base.GenerateDetayliCariEkstrePdfAsync(cari, hareketler, detaylar),
                $"Detayli_Ekstre_{cari.Unvan}.pdf");

        private async Task<(bool Success, string Error)> RunWithFallbackAsync(
            Func<Task<byte[]?>> httpCall,
            Func<Task<(bool Success, string Error)>> baseCall,
            string fileName)
        {
            var bytes = await httpCall();
            if (bytes == null || bytes.Length == 0) return await baseCall();
            return await SaveAndOpenAsync(fileName, bytes);
        }
    }
}
