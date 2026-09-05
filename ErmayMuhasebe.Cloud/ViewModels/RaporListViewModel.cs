using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Cloud.Services;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class RaporListViewModel : ErmayMuhasebe.Shared.ViewModels.RaporListViewModel
{
    private readonly ErmayMuhasebe.Cloud.Services.IFileService _fileService;

    public RaporListViewModel(IUnitOfWork uow, IPdfService pdfService, ErmayMuhasebe.Cloud.Services.IFileService fileService, IJSRuntime jsRuntime) 
        : base(uow, pdfService)
    {
        _fileService = fileService;
    }

    // Removed redundant overrides (RunReport, SelectCariAsync, etc.) 
    // as they are now handled by the Shared base class.

    protected override async Task GenerateReportInternal(ErmayMuhasebe.Shared.ViewModels.ReportItemViewModel item, CariKart? selectedCari = null, bool isPrint = false)
    {
        ErrorMessage = "";
        SuccessMessage = "Rapor verileri hazırlanıyor...";
        try 
        {
            IsGenerating = true;
            
            // Re-use core prediction/recalculation logic if needed (optional but good for consistency)
            // _ = Task.Run(async () => { try { await _uow.RecalculateSystemBalancesAsync(); } catch { } });

            if (item.Title == "Genel Özet" && selectedCari == null) 
            {
                var sections = new List<ReportSection>();
                var summaryData = await CalculateReportDataAsync(item);
                sections.Add(new ReportSection { Title = summaryData.Title, Headers = summaryData.Headers, Rows = summaryData.Rows });

                var budgetReportItem = GetAllReports().FirstOrDefault(r => r.Title == "Bütçe / Hedef Takibi");
                if (budgetReportItem != null)
                {
                    SuccessMessage = "Bütçe verileri analiz ediliyor...";
                    var res = await CalculateReportDataAsync(budgetReportItem);
                    var budgetGraphData = await GetBudgetDataAsync();
                    
                    // 1. Aylık Grafik ve Tablo
                    sections.Add(new ReportSection { 
                        Title = "BÜTÇE VE HEDEF ANALİZİ (AYLIK)", 
                        Headers = res.Headers, 
                        Rows = res.Rows, 
                        ChartData = null, // Charts not handled in Consolidated PDF API for now (placeholders)
                        NewPage = true 
                    });

                    // 2. Haftalık Grafik (Tablosuz)
                    if (budgetGraphData.Weekly.Any())
                    {
                         sections.Add(new ReportSection { 
                            Title = "BÜTÇE VE HEDEF ANALİZİ (HAFTALIK)", 
                            Headers = Array.Empty<string>(), 
                            Rows = new List<string[]>(), 
                            ChartData = null,
                            NewPage = false 
                        });
                    }
                }

                SuccessMessage = "Diğer raporlar paketleniyor...";
                foreach(var report in GetAllReports().Where(r => r.Title != "Genel Özet" && r.Title != "Bütçe / Hedef Takibi"))
                {
                    try 
                    {
                        var (title, headers, rows) = await CalculateReportDataAsync(report);
                        if (rows != null && rows.Count > 0)
                        {
                            sections.Add(new ReportSection { 
                                Title = title, 
                                Headers = headers, 
                                Rows = rows, 
                                ChartData = null,
                                NewPage = true 
                            });
                        }
                    } catch { }
                }

                SuccessMessage = "PDF dosyası oluşturuluyor...";
                var (success, error) = await _pdfService.GenerateConsolidatedReportPdfAsync("ERMAY MUHASEBE - GENEL RAPOR PAKETİ", sections);
                if (success) 
                {
                    SuccessMessage = "Rapor başarıyla oluşturuldu.";
                }
                else 
                {
                    ErrorMessage = $"PDF API hatası: {error}. API'nin (http://localhost:5244) çalıştığından emin olun.";
                    SuccessMessage = "";
                }
                return;
            }
            
            if (item.Title == "Bütçe / Hedef Takibi")
            {
                SuccessMessage = "Bütçe verileri hesaplanıyor...";
                var budgetData = await GetBudgetDataAsync();
                
                var annual = budgetData.Annual.Select(x => new ChartDataItem { Label = x.Label, Target = x.Target, Actual = x.Actual }).ToList();
                var monthly = budgetData.Monthly.Select(x => new ChartDataItem { Label = x.Label, Target = x.Target, Actual = x.Actual }).ToList();
                var weekly = budgetData.Weekly.Select(x => new ChartDataItem { Label = x.Label, Target = x.Target, Actual = x.Actual }).ToList();

                SuccessMessage = "Bütçe raporu PDF'e dönüştürülüyor...";
                var (success, error) = await _pdfService.GenerateBudgetReportPdfAsync("BÜTÇE VE HEDEF ANALİZ RAPORU", annual, monthly, weekly);
                if (success) 
                {
                    SuccessMessage = "Rapor başarıyla oluşturuldu.";
                }
                else 
                {
                    ErrorMessage = $"PDF API hatası: {error}";
                    SuccessMessage = "";
                }
                return;
            }

            SuccessMessage = $"{item.Title} verileri çekiliyor...";
            var (rTitle, rHeaders, rRows) = await CalculateReportDataAsync(item, selectedCari);
            
            if (rRows == null || !rRows.Any())
            {
                ErrorMessage = "Raporlanacak veri bulunamadı.";
                SuccessMessage = "";
                return;
            }

            SuccessMessage = "Rapor sunucuya gönderiliyor...";
            // Call API via CloudPdfService instead of direct JS
            var (finalSuccess, finalError) = await _pdfService.GenerateGenericTablePdfAsync(rTitle, rHeaders, rRows);
            if (finalSuccess) 
            {
                SuccessMessage = "Rapor başarıyla oluşturuldu.";
            }
            else 
            {
                ErrorMessage = $"PDF API hatası: {finalError}";
                SuccessMessage = "";
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Rapor Hatası: {ex.Message}";
            SuccessMessage = "";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    protected override async Task HandleFileOpenAsync(byte[] content, string fileName)
    {
        // Using Download instead of Open as it's more reliable in WASM
        await _fileService.DownloadFileAsync(fileName, "application/pdf", content);
    }

    protected override async Task HandleFilePrintAsync(byte[] content, string fileName)
    {
        // In cloud/web, we just fallback to Open/Download for now
        await HandleFileOpenAsync(content, fileName);
    }
}
