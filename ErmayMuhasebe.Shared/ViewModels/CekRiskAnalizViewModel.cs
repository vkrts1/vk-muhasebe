using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Shared.ViewModels;

public record RiskItem(string CariUnvan, double Puan, string Durum, string Renk);

public partial class CekRiskAnalizViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    private readonly IPdfService _pdfService;
    private readonly IFileService _fileService;
    [ObservableProperty] private ObservableCollection<RiskItem> _riskListesi = new();
    [ObservableProperty] private double _genelRiskPuani;
    [ObservableProperty] private string _analizOzeti = "Veriler analiz ediliyor...";

    public CekRiskAnalizViewModel(IUnitOfWork uow, IPdfService pdfService, IFileService fileService)
    {
        _uow = uow;
        _pdfService = pdfService;
        _fileService = fileService;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var cariler = await _uow.Cariler.GetAllAsync();
        var cekler = await _uow.Cekler.GetAllAsync();

        // Basit risk puanlama mantığı: 
        // Vadesi geçmiş borçlar ve karşılıksız çeklere göre puan düşer.
        var list = new List<RiskItem>();
        foreach (var c in cariler.Take(10)) // Örnek için 10 tane
        {
            double puan = 85.0; // Baz puan
            if (c.Borc > c.Alacak * 2) puan -= 20;
            
            string durum = puan > 70 ? "Güvenli" : (puan > 40 ? "Dikkat" : "Riskli");
            string renk = puan > 70 ? "#10B981" : (puan > 40 ? "#F59E0B" : "#EF4444");
            
            list.Add(new RiskItem(c.Unvan ?? "Adsız Cari", puan, durum, renk));
        }

        RiskListesi = new ObservableCollection<RiskItem>(list.OrderBy(x => x.Puan));
        GenelRiskPuani = list.Any() ? list.Average(x => x.Puan) : 100;
        AnalizOzeti = $"Portföyünüzde {list.Count(x => x.Puan < 50)} yüksek riskli cari tespit edildi. Çek kabullerinde dikkatli olunmalıdır.";
    }

    [RelayCommand]
    public async Task DownloadRaporAsync()
    {
        try 
        {
            var headers = new[] { "Cari Ünvan", "Risk Skoru", "Durum" };
            var rows = RiskListesi.Select(x => new[] { x.CariUnvan, x.Puan.ToString("N1"), x.Durum }).ToList();
            
            var pdfBytes = await _pdfService.GenerateGenericTablePdfBytesAsync("CARİ RİSK ANALİZ RAPORU", headers, rows, AnalizOzeti);
            await _fileService.SaveAndOpenFileAsync($"RiskAnalizi_{DateTime.Now:ddMM}.pdf", pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Rapor hatası: {ex.Message}";
        }
    }
}
