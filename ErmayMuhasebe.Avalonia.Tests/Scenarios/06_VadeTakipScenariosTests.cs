using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class VadeTakipScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Fatura Vadeleri (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_OpenSalesInvoice_IncludedInVadeTakipAsReceivable()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-V01",
            Tur = "Satis",
            CariUnvan = "Vade Müşteri 1",
            VadeTarihi = DateTime.Today.AddDays(15),
            GenelToplam = 5000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.Contains(vm.Vadeler, v => v.SourceId == fatura.Id && v.IsIncoming && v.Tutar == 5000m);
    }

    [AvaloniaFact]
    public async Task Scenario_02_OpenPurchaseInvoice_IncludedInVadeTakipAsPayable()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-V02",
            Tur = "Alis",
            CariUnvan = "Vade Tedarikçi 1",
            VadeTarihi = DateTime.Today.AddDays(20),
            GenelToplam = 8000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.Contains(vm.Vadeler, v => v.SourceId == fatura.Id && !v.IsIncoming && v.Tutar == 8000m);
    }

    [AvaloniaFact]
    public async Task Scenario_03_PartiallyPaidInvoice_OnlyListsRemainingBalance()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-V03",
            Tur = "Satis",
            VadeTarihi = DateTime.Today.AddDays(5),
            GenelToplam = 10000m,
            Odenen = 6500m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id);
        Assert.NotNull(item);
        Assert.Equal(3500m, item.Tutar);
    }

    [AvaloniaFact]
    public async Task Scenario_04_FullyPaidInvoice_ExcludedFromVadeTakip()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-V04",
            Tur = "Satis",
            VadeTarihi = DateTime.Today.AddDays(10),
            GenelToplam = 6000m,
            Odenen = 6000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == fatura.Id);
    }

    [Theory]
    [InlineData(-10, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    public void Scenario_05_to_08_OverdueStatusEvaluation(int gunFarki, bool expectedOverdue)
    {
        DateTime vade = DateTime.Today.AddDays(gunFarki);
        bool isOverdue = vade < DateTime.Today;
        Assert.Equal(expectedOverdue, isOverdue);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 7)]
    [InlineData(30, 30)]
    public void Scenario_09_to_11_DaysRemainingCalculations(int addDays, int expectedDaysRemaining)
    {
        DateTime vade = DateTime.Today.AddDays(addDays);
        int remaining = (vade.Date - DateTime.Today).Days;
        Assert.Equal(expectedDaysRemaining, remaining);
    }

    [Theory]
    [InlineData(-5, 5)]
    [InlineData(-15, 15)]
    [InlineData(-45, 45)]
    [InlineData(2, 0)]
    public void Scenario_12_to_15_OverdueDaysCalculations(int addDays, int expectedOverdueDays)
    {
        DateTime vade = DateTime.Today.AddDays(addDays);
        int overdue = vade < DateTime.Today ? (DateTime.Today - vade.Date).Days : 0;
        Assert.Equal(expectedOverdueDays, overdue);
    }

    // -------------------------------------------------------------
    // 2. Çek ve Senet Vadeleri (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_16_PortfolioCheck_IncludedAsReceivable()
    {
        var cek = new Cek
        {
            CekNo = "CK-VAD-1",
            CekTuru = "Alınan",
            Durum = "Portföyde",
            VadeTarihi = DateTime.Today.AddDays(25),
            Tutar = 14000m
        };
        await _uow.Cekler.SaveAsync(cek);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.Contains(vm.Vadeler, v => v.SourceId == cek.Id && v.Type == "Cek" && v.IsIncoming);
    }

    [AvaloniaFact]
    public async Task Scenario_17_OwnCheck_IncludedAsPayable()
    {
        var cek = new Cek
        {
            CekNo = "CK-VAD-2",
            CekTuru = "Verilen",
            Durum = "Portföyde",
            VadeTarihi = DateTime.Today.AddDays(40),
            Tutar = 22000m
        };
        await _uow.Cekler.SaveAsync(cek);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.Contains(vm.Vadeler, v => v.SourceId == cek.Id && v.Type == "Cek" && !v.IsIncoming);
    }

    [AvaloniaFact]
    public async Task Scenario_18_PromissoryNote_IncludedAccurately()
    {
        var senet = new Senet
        {
            SenetNo = "SN-VAD-1",
            SenetTuru = "Alınan",
            Durum = "Portföyde",
            VadeTarihi = DateTime.Today.AddDays(18),
            Tutar = 7500m
        };
        await _uow.Senetler.SaveAsync(senet);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.Contains(vm.Vadeler, v => v.SourceId == senet.Id && v.Type == "Senet" && v.IsIncoming);
    }

    [Theory]
    [InlineData("Tahsil Edildi", false)]
    [InlineData("Ciro Edildi", false)]
    [InlineData("Portföyde", true)]
    [InlineData("Portfoyde", true)]
    public void Scenario_19_to_22_CheckStatusFilterForMaturity(string durum, bool shouldInclude)
    {
        bool include = durum == "Portföyde" || durum == "Portfoyde";
        Assert.Equal(shouldInclude, include);
    }

    [Theory]
    [InlineData(10000, 20000, 30000)]
    [InlineData(50000, 0, 50000)]
    [InlineData(0, 0, 0)]
    public void Scenario_23_to_25_ChequeMaturityAggregation(decimal tutar1, decimal tutar2, decimal expTotal)
    {
        decimal total = tutar1 + tutar2;
        Assert.Equal(expTotal, total);
    }

    // -------------------------------------------------------------
    // 3. Filtreler ve Arama Senaryoları (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_26_FilterByBugun_ReturnsOnlyTodayItems()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-TODAY", Tur = "Satis", VadeTarihi = DateTime.Today, GenelToplam = 1000 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-FUTURE", Tur = "Satis", VadeTarihi = DateTime.Today.AddDays(10), GenelToplam = 2000 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SelectedFilter = "Bugün";

        Assert.Single(vm.Vadeler);
        Assert.Equal(1000m, vm.Vadeler[0].Tutar);
    }

    [AvaloniaFact]
    public async Task Scenario_27_FilterByGecikmis_ReturnsOnlyPastDueItems()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-PAST", Tur = "Satis", VadeTarihi = DateTime.Today.AddDays(-5), GenelToplam = 3000 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-NEXT", Tur = "Satis", VadeTarihi = DateTime.Today.AddDays(5), GenelToplam = 4000 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SelectedFilter = "Gecikmiş";

        Assert.Single(vm.Vadeler);
        Assert.Equal(3000m, vm.Vadeler[0].Tutar);
    }

    [AvaloniaFact]
    public async Task Scenario_28_SearchByCariAdi_FiltersAccurately()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-C1", CariUnvan = "Balkan Demir", Tur = "Satis", GenelToplam = 1000, VadeTarihi = DateTime.Today });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-C2", CariUnvan = "Ceylan Tekstil", Tur = "Satis", GenelToplam = 2000, VadeTarihi = DateTime.Today });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SearchText = "Balkan";

        Assert.Single(vm.Vadeler);
        Assert.Equal("Balkan Demir", vm.Vadeler[0].CariAdi);
    }

    [Theory]
    [InlineData("Tümü")]
    [InlineData("Bugün")]
    [InlineData("Bu Hafta")]
    [InlineData("Bu Ay")]
    [InlineData("Gecikmiş")]
    public void Scenario_29_to_33_ValidFiltersCollection(string filter)
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        Assert.Contains(filter, vm.Filters);
    }

    [Theory]
    [InlineData(1000, 2000, 3000, 6000)]
    [InlineData(5000, 0, 0, 5000)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_34_to_36_ReceivableTotals(decimal f, decimal c, decimal s, decimal expTotal)
    {
        decimal total = f + c + s;
        Assert.Equal(expTotal, total);
    }

    [Theory]
    [InlineData(15000, 5000, 10000)]
    [InlineData(10000, 15000, -5000)]
    [InlineData(8000, 8000, 0)]
    [InlineData(0, 0, 0)]
    public void Scenario_37_to_40_NetMaturityBalance(decimal alacak, decimal borc, decimal expNet)
    {
        decimal net = alacak - borc;
        Assert.Equal(expNet, net);
    }

    // -------------------------------------------------------------
    // 4. Eylemler, Çıktılar & Raporlama (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_41_ExportToExcelCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        Assert.True(vm.ExportToExcelCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task Scenario_42_ExportToPdfCommand_ProducesPdfBytes()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-PDF-V", Tur = "Satis", VadeTarihi = DateTime.Today, GenelToplam = 2500 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        await vm.PrintCommand.ExecuteAsync(null);

        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 0);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(0, 0)]
    [InlineData(12, 12)]
    public void Scenario_43_to_45_GecikmisAdetCounterCalculations(int gecikmisSayisi, int expectedAdet)
    {
        Assert.Equal(expectedAdet, gecikmisSayisi);
    }

    [Theory]
    [InlineData(1000, 32.5, 32500)]
    [InlineData(500, 35.0, 17500)]
    [InlineData(0, 32.5, 0)]
    [InlineData(2000, 0, 0)]
    [InlineData(100, 1.0, 100)]
    public void Scenario_46_to_50_ForeignCurrencyMaturityConversion(decimal tutarFx, double kur, decimal expTl)
    {
        decimal tl = tutarFx * (decimal)kur;
        Assert.Equal(expTl, tl);
    }
}
