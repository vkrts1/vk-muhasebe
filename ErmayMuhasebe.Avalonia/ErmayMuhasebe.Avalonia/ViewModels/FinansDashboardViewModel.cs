using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using SVM = ErmayMuhasebe.Shared.ViewModels;
using Avalonia.Threading;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class FinansDashboardViewModel : SVM.FinansDashboardViewModel
{

    [ObservableProperty] private ISeries[] _trendSeries;
    [ObservableProperty] private Axis[] _trendXAxis;
    [ObservableProperty] private Axis[] _trendYAxis;
    [ObservableProperty] private SolidColorPaint _legendPaint;

    public FinansDashboardViewModel(IUnitOfWork uow) : base(uow)
    {
        TrendSeries = Array.Empty<ISeries>();
        TrendXAxis = new Axis[] { new Axis() };
        TrendYAxis = new Axis[] 
        { 
            new Axis 
            { 
                Labeler = value => value.ToString("N0") + " ₺",
                LabelsPaint = new SolidColorPaint(new SKColor(156, 163, 175)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 5))
            } 
        };
        LegendPaint = new SolidColorPaint(new SKColor(241, 245, 249));
    }

    protected override async Task InvokeOnUIThreadAsync(Action action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
    }

    protected override async Task LoadFinansTrendChartAsync()
    {
        await base.LoadFinansTrendChartAsync();
        
        var data = FinansalTrendData;
        if (data == null || data.Length < 3) return;

        var incValues = data[0].Values;
        var redValues = data[1].Values;
        var expValues = data[2].Values;
        var labels = data[0].Labels;

        // --- PREMIUM NEON PALETTE ---
        var incColor = new SKColor(0, 255, 163);   // Neon Emerald
        var redColor = new SKColor(0, 212, 255);   // Electric Blue
        var expColor = new SKColor(255, 77, 77);   // Hot Red
        var labelColor = new SKColor(148, 163, 184); // Slate 400

        await InvokeOnUIThreadAsync(() => 
        {
            TrendSeries = new ISeries[]
            {
                // Gelen Tahsilatlar (Premium Line Chart - Matched with TargetVsActual)
                new LineSeries<decimal>
                {
                    Name = "Gelen Tahsilatlar",
                    Values = incValues,
                    Stroke = new SolidColorPaint(incColor, 3),
                    Fill = new LinearGradientPaint(
                        new SKColor[] { incColor.WithAlpha(60), incColor.WithAlpha(0) },
                        new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    GeometrySize = 8,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(incColor, 3),
                    LineSmoothness = 0.6,
                    EasingFunction = LiveChartsCore.EasingFunctions.ExponentialOut,
                    AnimationsSpeed = TimeSpan.FromMilliseconds(1000)
                },
                // Yönlendirilen Tahsilatlar
                new LineSeries<decimal>
                {
                    Name = "Yönlendirilen Tahsilatlar",
                    Values = redValues,
                    Stroke = new SolidColorPaint(redColor, 3),
                    Fill = new LinearGradientPaint(
                        new SKColor[] { redColor.WithAlpha(60), redColor.WithAlpha(0) },
                        new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    GeometrySize = 8,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(redColor, 3),
                    LineSmoothness = 0.6,
                    EasingFunction = LiveChartsCore.EasingFunctions.ExponentialOut,
                    AnimationsSpeed = TimeSpan.FromMilliseconds(1000)
                },
                // Ödemeler
                new LineSeries<decimal>
                {
                    Name = "Ödemeler",
                    Values = expValues,
                    Stroke = new SolidColorPaint(expColor, 3),
                    Fill = new LinearGradientPaint(
                        new SKColor[] { expColor.WithAlpha(60), expColor.WithAlpha(0) },
                        new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    GeometrySize = 8,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(expColor, 3),
                    LineSmoothness = 0.6,
                    EasingFunction = LiveChartsCore.EasingFunctions.ExponentialOut,
                    AnimationsSpeed = TimeSpan.FromMilliseconds(1000)
                }
            };

            TrendXAxis = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(labelColor),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 8)),
                    Padding = new LiveChartsCore.Drawing.Padding(0, 15, 0, 0)
                }
            };

            TrendYAxis = new Axis[] 
            { 
                new Axis 
                { 
                    Labeler = value => value.ToString("N0") + " ₺",
                    LabelsPaint = new SolidColorPaint(labelColor),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(255, 255, 255, 8)),
                    ShowSeparatorLines = true
                } 
            };
        });
    }
}
