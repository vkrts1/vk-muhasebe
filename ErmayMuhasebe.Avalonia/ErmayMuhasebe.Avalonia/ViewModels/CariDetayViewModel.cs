using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Kernel.Sketches;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class CariDetayViewModel : ErmayMuhasebe.Shared.ViewModels.CariDetayViewModel
{
    [ObservableProperty] private ISeries[] _maturityTimelineSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ICartesianAxis[] _maturityTimelineXAxis = Array.Empty<ICartesianAxis>();
    [ObservableProperty] private ICartesianAxis[] _maturityTimelineYAxis = Array.Empty<ICartesianAxis>();
    [ObservableProperty] private ISeries[] _agingSeries = Array.Empty<ISeries>();
    [ObservableProperty] private SolidColorPaint _legendTextPaint = new() { Color = new SKColor(220, 220, 220) };

    public CariDetayViewModel(IUnitOfWork uow, IPdfService pdfService) 
        : base(uow, pdfService)
    {
    }

    public override void GoBack()
    {
        OnRequestClose();
    }

    public override async Task LoadExtendedDetailsAsync()
    {
        await base.LoadExtendedDetailsAsync();
        
        try
        {
            UpdateMaturityTimelineChart();
            UpdateAgingDonutChart();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating charts in CariDetayViewModel: {ex.Message}");
        }
    }

    private void UpdateMaturityTimelineChart()
    {
        var today = DateTime.Today;
        
        // 5 bins: 
        // 0: 0-7 days
        // 1: 8-14 days
        // 2: 15-21 days
        // 3: 22-30 days
        // 4: 30+ days
        var incomingValues = new decimal[5];
        var outgoingValues = new decimal[5];
        var labels = new string[] { "0-7 Gün", "8-14 Gün", "15-21 Gün", "22-30 Gün", "30+ Gün" };

        // Group future invoices
        foreach (var f in Faturalar.Where(f => f.Kalan > 0 && f.VadeTarihi.Date >= today))
        {
            int days = (f.VadeTarihi.Date - today).Days;
            int binIndex = GetBinIndex(days);

            if (f.IsIncoming)
                incomingValues[binIndex] += f.Kalan;
            else
                outgoingValues[binIndex] += f.Kalan;
        }

        // Group future checks and bills
        foreach (var ev in BekleyenEvraklar.Where(e => e.VadeTarihi.Date >= today))
        {
            int days = (ev.VadeTarihi.Date - today).Days;
            int binIndex = GetBinIndex(days);

            if (ev.Tur == "Alınan")
                incomingValues[binIndex] += ev.Tutar;
            else
                outgoingValues[binIndex] += ev.Tutar;
        }

        var incColor = new SKColor(16, 185, 129); // Emerald Green (#10B981)
        var outColor = new SKColor(239, 68, 68); // Red (#EF4444)

        MaturityTimelineSeries = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Name = "Gelecek Tahsilat",
                Values = incomingValues,
                Fill = new SolidColorPaint(incColor),
                MaxBarWidth = 25
            },
            new ColumnSeries<decimal>
            {
                Name = "Gelecek Ödeme",
                Values = outgoingValues,
                Fill = new SolidColorPaint(outColor),
                MaxBarWidth = 25
            }
        };

        MaturityTimelineXAxis = new ICartesianAxis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 11
            }
        };

        MaturityTimelineYAxis = new ICartesianAxis[]
        {
            new Axis
            {
                MinLimit = 0,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(15)) { StrokeThickness = 1 },
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 11
            }
        };
    }

    private int GetBinIndex(int days)
    {
        if (days <= 7) return 0;
        if (days <= 14) return 1;
        if (days <= 21) return 2;
        if (days <= 30) return 3;
        return 4;
    }

    private void UpdateAgingDonutChart()
    {
        var today = DateTime.Today;
        decimal bin1 = 0; // 1-30 days overdue
        decimal bin2 = 0; // 31-60 days overdue
        decimal bin3 = 0; // 60+ days overdue

        // Add overdue invoices
        foreach (var f in Faturalar.Where(f => f.Kalan > 0 && f.VadeTarihi.Date < today))
        {
            int delay = (today - f.VadeTarihi.Date).Days;
            if (delay <= 30) bin1 += f.Kalan;
            else if (delay <= 60) bin2 += f.Kalan;
            else bin3 += f.Kalan;
        }

        // Add overdue checks and bills
        foreach (var ev in BekleyenEvraklar.Where(e => e.VadeTarihi.Date < today))
        {
            int delay = (today - ev.VadeTarihi.Date).Days;
            if (delay <= 30) bin1 += ev.Tutar;
            else if (delay <= 60) bin2 += ev.Tutar;
            else bin3 += ev.Tutar;
        }

        if (bin1 == 0 && bin2 == 0 && bin3 == 0)
        {
            AgingSeries = new ISeries[]
            {
                new PieSeries<decimal>
                {
                    Name = "Vadesi Geçen Borç/Alacak Yok",
                    Values = new decimal[] { 1 },
                    Fill = new SolidColorPaint(new SKColor(16, 185, 129, 120)), // Green
                    InnerRadius = 55
                }
            };
        }
        else
        {
            AgingSeries = new ISeries[]
            {
                new PieSeries<decimal>
                {
                    Name = "1-30 Gün",
                    Values = new decimal[] { bin1 },
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11)), // Amber (#F59E0B)
                    InnerRadius = 55
                },
                new PieSeries<decimal>
                {
                    Name = "31-60 Gün",
                    Values = new decimal[] { bin2 },
                    Fill = new SolidColorPaint(new SKColor(249, 115, 22)), // Orange (#F97316)
                    InnerRadius = 55
                },
                new PieSeries<decimal>
                {
                    Name = "60+ Gün",
                    Values = new decimal[] { bin3 },
                    Fill = new SolidColorPaint(new SKColor(239, 68, 68)), // Red (#EF4444)
                    InnerRadius = 55
                }
            };
        }
    }

    [RelayCommand]
    public Task OpenEditCariAsync()
    {
        return Task.CompletedTask;
    }
}
