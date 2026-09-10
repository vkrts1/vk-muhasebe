using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Models
{
    // Data Transfer Objects for PDF API
    public record FaturaRequest(Fatura Fatura, List<FaturaDetay> Detaylar, byte[]? LogoBytes = null, bool ShowLogo = true, string? Size = null, string? Orientation = null, FaturaTasarimi? Tasarim = null);
    public record TeklifRequest(Teklif Teklif, List<TeklifDetay> Detaylar, CariKart? Cari, byte[]? LogoBytes = null, bool ShowLogo = true, string? Size = null, string? Orientation = null);
    public record SiparisRequest(Siparis Siparis, List<SiparisDetay> Detaylar, CariKart? Cari, byte[]? LogoBytes = null, bool ShowLogo = true, string? Size = null, string? Orientation = null);
    public record GenericTableRequest(string Title, string[] Headers, List<string[]> Rows, string? Subtitle = null, byte[]? LogoBytes = null, bool ShowLogo = true);
    
    public record ConsolidatedReportRequest(string Title, List<ErmayMuhasebe.Services.ReportSection> Sections, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record BudgetReportRequest(string Title, List<ErmayMuhasebe.Services.ChartDataItem> Annual, List<ErmayMuhasebe.Services.ChartDataItem> Monthly, List<ErmayMuhasebe.Services.ChartDataItem> Weekly, byte[]? LogoBytes = null, bool ShowLogo = true);
    
    // Stok report DTOs for mobile parity
    public record StokListReportRequest(List<StokKart> Stoklar, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record StokHareketReportRequest(StokKart Stok, List<StokHareket> Hareketler, byte[]? LogoBytes = null, bool ShowLogo = true);
    
    public record MakbuzRequest(string? MakbuzTipi, string? CariUnvan, DateTime Tarih, decimal Tutar, string? Aciklama, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record KasaMakbuzRequest(KasaHareket Hareket, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record EftRequest(EftIslem Islem, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record KkRequest(KrediKartiIslem Islem, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record EkstreRequest(CariKart Cari, List<CariHareket> Hareketler, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record DetayliEkstreRequest(CariKart Cari, List<CariHareket> Hareketler, Dictionary<int, List<FaturaDetay>> DetayDictionary, byte[]? LogoBytes = null, bool ShowLogo = true);

    // New PDF Request DTOs for complete mobile/desktop parity
    public record KasaEkstreRequest(BankaKart Kasa, List<KasaHareket> Hareketler, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record CekRequest(Cek Cek, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record CekListRequest(List<Cek> Cekler, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record KrediKartiListRequest(List<KrediKartiIslem> Islemler, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record EftListRequest(List<EftIslem> Islemler, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record KasaListRequest(List<BankaKart> Kasalar, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record FaturaListRequest(List<Fatura> Faturalar, DateTime StartDate, DateTime EndDate, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record VadeRaporuRequest(List<VadeReportItem> Items, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record AdresEtiketiRequest(string Unvan, string AdSoyad, string Adres, string IlIlce, string PostaKodu, string Telefon, byte[]? LogoBytes = null, bool ShowLogo = true);
    public record FinansRaporRequest(string Title, FinancialReportData Data, byte[]? LogoBytes = null, bool ShowLogo = true);
}

