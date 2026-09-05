using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Cloud.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

using Microsoft.JSInterop;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class FaturaListViewModel : ErmayMuhasebe.Shared.ViewModels.FaturaListViewModel
{
    private readonly ErmayMuhasebe.Cloud.Services.IFileService _fileService;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _js;

    public FaturaListViewModel(
        IUnitOfWork uow,
        IExcelService excelService,
        IPdfService pdfService,
        ErmayMuhasebe.Cloud.Services.IFileService fileService,
        NavigationManager navigationManager,
        IJSRuntime js)
        : base(uow, excelService, pdfService)
    {
        _fileService = fileService;
        _navigationManager = navigationManager;
        _js = js;
    }

    protected override void NotifyFinancialDataChanged()
    {
        // In Blazor, we can just use the DB service's state or a shared state service
        // For now, no specific message needed as pages refresh on navigation
    }

    protected override async Task HandleFileOpenAsync(byte[] content, string fileName)
    {
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            await _fileService.OpenPdfAsync(content);
        }
        else
        {
            string contentType = fileName.EndsWith(".xlsx") ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/octet-stream";
            await _fileService.DownloadFileAsync(fileName, contentType, content);
        }
    }

    public override async Task EditFaturaAsync(Fatura? fatura)
    {
        if (fatura == null) return;
        _navigationManager.NavigateTo($"/fatura-olustur/{(fatura.Tur == "Alış" ? "Alis" : "Satis")}/{fatura.CariId}?Id={fatura.Id}");
        await Task.CompletedTask;
    }

    public override void OpenCreateInvoice(string type)
    {
        _navigationManager.NavigateTo($"/fatura-olustur/{type}");
    }

    public override async Task ViewFaturaPdfAsync(Fatura? fatura)
    {
        if (fatura == null) return;
        try 
        {
            var detaylar = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
            // API üzerinden PDF üret
            var (success, error) = await _pdfService.GenerateFaturaPdfAsync(fatura, detaylar);
            if (!success) ErrorMessage = $"Cloud PDF Hatası: {error}";
        }
        catch (Exception ex) 
        {
            ErrorMessage = $"Cloud PDF Hatası: {ex.Message}";
        }
    }

    public override async Task ExportAllToPdfAsync()
    {
        if (!Faturalar.Any()) return;
        try 
        {
            var headers = new[] { "Fatura No", "Tarih", "Cari Hesap", "Tür", "Genel Toplam" };
            var rows = Faturalar.Select(f => new[] { 
                f.FaturaNo ?? "", 
                f.Tarih.ToString("dd.MM.yyyy"), 
                f.CariUnvan ?? "", 
                f.Tur ?? "", 
                f.GenelToplam.ToString("N2") + " ₺" 
            }).ToList();
            
            var (success, error) = await _pdfService.GenerateGenericTablePdfAsync(
                "Fatura Listesi", 
                headers, 
                rows, 
                $"Tarih Aralığı: {StartDateStr} - {EndDateStr}");

            if (!success) ErrorMessage = $"Cloud PDF Hatası: {error}";
        }
        catch (Exception ex) 
        {
            ErrorMessage = $"Liste PDF oluşturulurken hata: {ex.Message}";
        }
    }
}
