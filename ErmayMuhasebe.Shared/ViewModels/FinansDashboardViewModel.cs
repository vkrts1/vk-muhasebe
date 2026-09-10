using ErmayMuhasebe.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class FinansDashboardViewModel : DashboardViewModel
{
    [ObservableProperty] private decimal _musteriTahsilat;
    [ObservableProperty] private decimal _tedarikciOdeme;
    [ObservableProperty] private decimal _kasaToplam;
    [ObservableProperty] private decimal _bankaToplam;
    [ObservableProperty] private decimal _cekToplam;
    [ObservableProperty] private decimal _senetToplam;
    [ObservableProperty] private decimal _netBakiye;

    // === Ciro vs Kendi Kasamızda Kalan Analizi ===
    [ObservableProperty] private decimal _kendiNakit;
    [ObservableProperty] private decimal _kendiKK;
    [ObservableProperty] private decimal _kendiEFT;
    [ObservableProperty] private decimal _kendiCek;

    [ObservableProperty] private decimal _yonlendirilenNakit;
    [ObservableProperty] private decimal _yonlendirilenKK;
    [ObservableProperty] private decimal _yonlendirilenEFT;
    [ObservableProperty] private decimal _yonlendirilenCek;

    // === Alış Faturaları Kapatma Durumu ===
    [ObservableProperty] private decimal _alisFaturaToplam;
    [ObservableProperty] private decimal _alisFaturaOdenen;
    [ObservableProperty] private decimal _alisFaturaKalan;
    [ObservableProperty] private double _alisFaturaKapatmaOrani;
    [ObservableProperty] private decimal _alisFaturaGecikmis;
    [ObservableProperty] private decimal _alisFaturaOndenOdenen;

    // === Ortalama Vade Hızları ===
    [ObservableProperty] private double _ortalamaTahsilatGunu;
    [ObservableProperty] private double _ortalamaOdemeGunu;

    // === 30 Günlük Gelecek Projeksiyon Takvimi ===
    [ObservableProperty] private ObservableCollection<VadeItem> _gelecekHareketiListesi = new();

    // === Filtreleme Özellikleri ===
    [ObservableProperty] private string _activePeriodFilter = "Aylık"; // Haftalık, Aylık, 6 Aylık, Yıllık, Özel
    [ObservableProperty] private DateTime? _filterStartDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _filterEndDate = DateTime.Today;

    [ObservableProperty] private string _selectedPeriod = "Monthly"; // Weekly, Monthly, Yearly
    [ObservableProperty] private ChartDataSet[] _finansalTrendData = Array.Empty<ChartDataSet>();

    // === Quick Transaction Properties ===
    [ObservableProperty] private bool _isQuickTransactionVisible;
    [ObservableProperty] private string _quickTransactionTitle = "";
    [ObservableProperty] private string _quickTransactionType = ""; // "Tahsilat" or "Ödeme"
    [ObservableProperty] private decimal _quickAmount;
    [ObservableProperty] private string _quickDescription = "";
    [ObservableProperty] private DateTime _quickDate = DateTime.Now;
    [ObservableProperty] private string _quickDateStr = DateTime.Now.ToString("dd.MM.yyyy");
    [ObservableProperty] private string _quickPaymentMethod = "Nakit"; // Nakit, Havale / EFT, Kredi Kartı
    [ObservableProperty] private bool _isSaving;

    // Cari Selection
    [ObservableProperty] private bool _isCariSearchVisible;
    [ObservableProperty] private string _cariSearchText = "";
    [ObservableProperty] private ObservableCollection<CariKart> _searchedCariler = new();
    [ObservableProperty] private CariKart? _selectedQuickCari;

    // Kasa/Banka Selection
    [ObservableProperty] private ObservableCollection<BankaKart> _availableAccounts = new();
    [ObservableProperty] private BankaKart? _selectedAccount;

    // Payment method visibility
    [ObservableProperty] private bool _isNakitMethod = true;
    [ObservableProperty] private bool _isHavaleMethod;
    [ObservableProperty] private bool _isKrediKartiMethod;

    private bool _isFilterUpdating = false;

    [RelayCommand]
    public void SetPeriodFilter(string period)
    {
        _isFilterUpdating = true;
        ActivePeriodFilter = period;

        var today = DateTime.Today;
        switch (period)
        {
            case "Haftalık":
                FilterStartDate = today.AddDays(-7);
                FilterEndDate = today;
                break;
            case "Aylık":
                FilterStartDate = today.AddDays(-30);
                FilterEndDate = today;
                break;
            case "6 Aylık":
                FilterStartDate = today.AddMonths(-6);
                FilterEndDate = today;
                break;
            case "Yıllık":
                FilterStartDate = today.AddYears(-1);
                FilterEndDate = today;
                break;
            case "Özel":
                break;
        }
        _isFilterUpdating = false;
        _ = LoadFinansStatsAsync();
    }

    partial void OnFilterStartDateChanged(DateTime? value)
    {
        if (!_isFilterUpdating)
        {
            ActivePeriodFilter = "Özel";
            _ = LoadFinansStatsAsync();
        }
    }

    partial void OnFilterEndDateChanged(DateTime? value)
    {
        if (!_isFilterUpdating)
        {
            ActivePeriodFilter = "Özel";
            _ = LoadFinansStatsAsync();
        }
    }

    public FinansDashboardViewModel(IUnitOfWork uow) : base(uow)
    {
    }

    public override void OnNavigatedTo()
    {
        if (!DisableAutoRefresh)
        {
            base.OnNavigatedTo();
            _ = LoadFinansStatsAsync();
        }
    }

    [RelayCommand]
    public async Task ChangePeriod(string period)
    {
        SelectedPeriod = period;
        await LoadFinansTrendChartAsync();
    }

    public async Task LoadFinansStatsAsync()
    {
        try
        {
            var start = FilterStartDate ?? DateTime.Today.AddDays(-30);
            var end = FilterEndDate ?? DateTime.Today;

            var today = DateTime.Now;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Fetch Cari Harekets for specific stats
            var allHarekets = await _uow.Cariler.GetAllHareketlerAsync();
            var monthHarekets = allHarekets.Where(h => h.Tarih >= startOfMonth).ToList();

            MusteriTahsilat = monthHarekets
                .Where(h => h.IslemTuru != null && h.IslemTuru.Contains("Tahsilat"))
                .Sum(h => h.Alacak);

            TedarikciOdeme = monthHarekets
                .Where(h => h.IslemTuru != null && h.IslemTuru.Contains("Ödeme"))
                .Sum(h => h.Borc);

            // Account Totals (Current balance states)
            var bankalar = await _uow.Bankalar.GetAllAsync();
            KasaToplam = bankalar.Where(b => b.KartTuru == "Kasa").Sum(b => b.GuncelBakiye);
            BankaToplam = bankalar.Where(b => b.KartTuru != "Kasa").Sum(b => b.GuncelBakiye);

            var ceks = await _uow.Cekler.GetAllAsync();
            CekToplam = ceks.Where(c => c.Durum == "Portföyde" || c.Durum == "Portfoyde").Sum(c => c.Tutar);

            var senets = await _uow.Senetler.GetAllAsync();
            SenetToplam = senets.Where(s => s.Durum == "Portföyde" || s.Durum == "Portfoyde").Sum(s => s.Tutar);

            NetBakiye = KasaToplam + BankaToplam;

            // 1. Ciro / Kasada Kalan Analizi Hesaplamaları (Filtered by date range)
            var tahsilatHarekets = allHarekets.Where(h => h.Tarih >= start && h.Tarih <= end && h.Alacak > 0 && h.IslemTuru != null && !h.IslemTuru.Contains("Fatura")).ToList();

            KendiNakit = tahsilatHarekets.Where(h => h.YonlendirilenCariId == null && (h.IslemTuru ?? "").Contains("Nakit")).Sum(h => h.Alacak);
            KendiKK = tahsilatHarekets.Where(h => h.YonlendirilenCariId == null && (h.IslemTuru ?? "").Contains("KK")).Sum(h => h.Alacak);
            KendiEFT = tahsilatHarekets.Where(h => h.YonlendirilenCariId == null && ((h.IslemTuru ?? "").Contains("EFT") || (h.IslemTuru ?? "").Contains("Havale"))).Sum(h => h.Alacak);
            KendiCek = ceks.Where(c => c.VadeTarihi >= start && c.VadeTarihi <= end && c.YonlendirilenCariId == null && c.CekTuru != "Verilen" && c.Durum != "Kayıp" && c.Durum != "Iptal" && c.Durum != "İptal").Sum(c => c.Tutar);

            YonlendirilenNakit = tahsilatHarekets.Where(h => h.YonlendirilenCariId != null && (h.IslemTuru ?? "").Contains("Nakit")).Sum(h => h.Alacak);
            YonlendirilenKK = tahsilatHarekets.Where(h => h.YonlendirilenCariId != null && (h.IslemTuru ?? "").Contains("KK")).Sum(h => h.Alacak);
            YonlendirilenEFT = tahsilatHarekets.Where(h => h.YonlendirilenCariId != null && ((h.IslemTuru ?? "").Contains("EFT") || (h.IslemTuru ?? "").Contains("Havale"))).Sum(h => h.Alacak);
            YonlendirilenCek = ceks.Where(c => c.VadeTarihi >= start && c.VadeTarihi <= end && c.YonlendirilenCariId != null && c.CekTuru != "Verilen").Sum(c => c.Tutar);

            // 2. Alış Faturaları Kapatma Durumu (Filtered by date range)
            var faturalar = await _uow.Faturalar.GetAllAsync();
            
            // Recalculate invoice closure matches for caris with active invoices
            var activeInvoiceCariIds = faturalar.Where(f => !f.IsDeleted).Select(f => f.CariId).Distinct().ToList();
            foreach (var cid in activeInvoiceCariIds)
            {
                await _uow.Cariler.MatchInvoicePaymentsAsync(cid);
            }
            if (activeInvoiceCariIds.Any())
            {
                faturalar = await _uow.Faturalar.GetAllAsync();
            }

            var alisFaturalari = faturalar.Where(f => !f.IsDeleted && (f.Tur == "Alış" || f.Tur == "Alis")).ToList();

            AlisFaturaToplam = alisFaturalari.Sum(f => f.GenelToplam);
            AlisFaturaOdenen = alisFaturalari.Sum(f => f.Odenen);
            AlisFaturaKalan = alisFaturalari.Sum(f => f.Kalan);
            AlisFaturaKapatmaOrani = AlisFaturaToplam > 0 ? (double)(AlisFaturaOdenen / AlisFaturaToplam) * 100.0 : 0.0;
            AlisFaturaGecikmis = alisFaturalari.Where(f => f.VadeTarihi.Date < DateTime.Today && f.Kalan > 0).Sum(f => f.Kalan);
            AlisFaturaOndenOdenen = alisFaturalari.Where(f => f.VadeTarihi.Date >= DateTime.Today && f.Odenen > 0).Sum(f => f.Odenen);

            // 3. Ortalama Vade Hızları (Filtered by date range)
            var satisInvoices = faturalar.Where(f => !f.IsDeleted && (f.Tur == "Satış" || f.Tur == "Satis") && f.Tarih >= start && f.Tarih <= end).ToList();
            var customerPayments = allHarekets.Where(h => h.Tarih >= start && h.Tarih <= end && h.Alacak > 0 && h.IslemTuru != null && !h.IslemTuru.Contains("Fatura")).ToList();
            OrtalamaTahsilatGunu = CalculateAverageDays(satisInvoices, customerPayments);

            var supplierPayments = allHarekets.Where(h => h.Tarih >= start && h.Tarih <= end && h.Borc > 0 && h.IslemTuru != null && !h.IslemTuru.Contains("Fatura")).ToList();
            OrtalamaOdemeGunu = CalculateAverageDays(alisFaturalari, supplierPayments);

            // 4. 30 Günlük Gelecek Projeksiyon Takvimi (Next 30 days starting from today, or start from the filter end date if that is in the future)
            var projectionList = new List<VadeItem>();
            var startLimit = DateTime.Today;
            var endLimit = DateTime.Today.AddDays(30);

            // Gelecek Faturalar
            foreach (var f in faturalar.Where(x => !x.IsDeleted && x.Kalan > 0 && x.VadeTarihi >= startLimit && x.VadeTarihi <= endLimit))
            {
                projectionList.Add(new VadeItem
                {
                    Type = "Fatura",
                    SourceId = f.Id,
                    CariId = f.CariId,
                    Tur = f.Tur == "Satis" || f.Tur == "Satış" ? "Alacak (Fatura)" : "Borç (Fatura)",
                    CariAdi = f.CariUnvan ?? "Bilinmeyen",
                    VadeTarihi = f.VadeTarihi,
                    Tutar = f.Kalan,
                    IsIncoming = f.Tur == "Satis" || f.Tur == "Satış"
                });
            }

            // Gelecek Çekler
            foreach (var c in ceks.Where(x => (x.Durum == "Portföyde" || x.Durum == "Portfoyde") && x.VadeTarihi >= startLimit && x.VadeTarihi <= endLimit))
            {
                projectionList.Add(new VadeItem
                {
                    Type = "Cek",
                    SourceId = c.Id,
                    CariId = c.CariId ?? 0,
                    Tur = c.CekTuru == "Verilen" ? "Borç (Çek)" : "Alacak (Çek)",
                    CariAdi = c.CariUnvan ?? c.AsilBorclu ?? "Bilinmeyen",
                    VadeTarihi = c.VadeTarihi,
                    Tutar = c.Tutar,
                    IsIncoming = c.CekTuru != "Verilen"
                });
            }

            // Gelecek Senetler
            foreach (var s in senets.Where(x => (x.Durum == "Portföyde" || x.Durum == "Portfoyde") && x.VadeTarihi >= startLimit && x.VadeTarihi <= endLimit))
            {
                projectionList.Add(new VadeItem
                {
                    Type = "Senet",
                    SourceId = s.Id,
                    CariId = s.CariId ?? 0,
                    Tur = s.SenetTuru == "Verilen" ? "Borç (Senet)" : "Alacak (Senet)",
                    CariAdi = s.CariUnvan ?? s.AsilBorclu ?? "Bilinmeyen",
                    VadeTarihi = s.VadeTarihi,
                    Tutar = s.Tutar,
                    IsIncoming = s.SenetTuru != "Verilen"
                });
            }

            await InvokeOnUIThreadAsync(() =>
            {
                GelecekHareketiListesi = new ObservableCollection<VadeItem>(projectionList.OrderBy(x => x.VadeTarihi));
            });

            await LoadFinansTrendChartAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Finans Dashboard Error: {ex.Message}");
        }
    }

    private double CalculateAverageDays(List<Fatura> invoices, List<CariHareket> payments)
    {
        if (!invoices.Any() || !payments.Any()) return 0;

        var sortedInvoices = invoices.OrderBy(f => f.Tarih).Select(f => new { f.Tarih, f.GenelToplam }).ToList();
        var sortedPayments = payments.OrderBy(p => p.Tarih).Select(p => new { p.Tarih, Amount = p.Alacak > 0 ? p.Alacak : p.Borc }).ToList();

        double totalWeightedDays = 0;
        double totalMatchedAmount = 0;

        int invoiceIndex = 0;
        decimal invoiceRemaining = invoiceIndex < sortedInvoices.Count ? sortedInvoices[invoiceIndex].GenelToplam : 0;

        foreach (var payment in sortedPayments)
        {
            decimal paymentRemaining = payment.Amount;
            while (paymentRemaining > 0 && invoiceIndex < sortedInvoices.Count)
            {
                decimal matched = Math.Min(paymentRemaining, invoiceRemaining);
                double days = (payment.Tarih - sortedInvoices[invoiceIndex].Tarih).TotalDays;
                if (days < 0) days = 0;

                totalWeightedDays += days * (double)matched;
                totalMatchedAmount += (double)matched;

                paymentRemaining -= matched;
                invoiceRemaining -= matched;

                if (invoiceRemaining <= 0)
                {
                    invoiceIndex++;
                    if (invoiceIndex < sortedInvoices.Count)
                    {
                        invoiceRemaining = sortedInvoices[invoiceIndex].GenelToplam;
                    }
                }
            }
        }

        return totalMatchedAmount > 0 ? Math.Round(totalWeightedDays / totalMatchedAmount, 1) : 0;
    }

    protected virtual async Task LoadFinansTrendChartAsync()
    {
        var trendData = await _uow.GetFinanceTrendAsync(SelectedPeriod);
        
        var incValues = trendData.Select(x => x.Income).ToArray();
        var redValues = trendData.Select(x => x.Redirected).ToArray();
        var expValues = trendData.Select(x => x.Expense).ToArray();
        var labels = trendData.Select(x => x.Label).ToArray();

        await InvokeOnUIThreadAsync(() => {
            FinansalTrendData = new ChartDataSet[]
            {
                new ChartDataSet 
                { 
                    Name = "Gelen Tahsilatlar",
                    Values = incValues,
                    Labels = labels,
                    Color = "#00FFA3" // Neon Emerald
                },
                new ChartDataSet 
                { 
                    Name = "Yönlendirilen Tahsilatlar",
                    Values = redValues,
                    Labels = labels,
                    Color = "#00D4FF" // Electric Blue
                },
                new ChartDataSet 
                { 
                    Name = "Ödemeler",
                    Values = expValues,
                    Labels = labels,
                    Color = "#FF4D4D" // Hot Red
                }
            };
        });
    }

    // === Quick Transaction Commands ===

    [RelayCommand]
    public void OpenQuickTahsilat()
    {
        ResetQuickTransaction();
        QuickTransactionType = "Tahsilat";
        QuickTransactionTitle = "Hızlı Tahsilat";
        IsQuickTransactionVisible = true;
        _ = LoadCariListAsync();
        _ = LoadAccountsForMethodAsync("Nakit");
    }

    [RelayCommand]
    public void OpenQuickOdeme()
    {
        ResetQuickTransaction();
        QuickTransactionType = "Ödeme";
        QuickTransactionTitle = "Hızlı Ödeme";
        IsQuickTransactionVisible = true;
        _ = LoadCariListAsync();
        _ = LoadAccountsForMethodAsync("Nakit");
    }

    [RelayCommand]
    public void CloseQuickTransaction()
    {
        IsQuickTransactionVisible = false;
        ResetQuickTransaction();
    }

    private void ResetQuickTransaction()
    {
        QuickAmount = 0;
        QuickDescription = "";
        QuickDate = DateTime.Now;
        QuickDateStr = DateTime.Now.ToString("dd.MM.yyyy");
        QuickPaymentMethod = "Nakit";
        SelectedQuickCari = null;
        SelectedAccount = null;
        CariSearchText = "";
        ErrorMessage = "";
        SuccessMessage = "";
        IsNakitMethod = true;
        IsHavaleMethod = false;
        IsKrediKartiMethod = false;
    }

    partial void OnQuickPaymentMethodChanged(string value)
    {
        IsNakitMethod = value == "Nakit";
        IsHavaleMethod = value == "Havale / EFT";
        IsKrediKartiMethod = value == "Kredi Kartı";
        _ = LoadAccountsForMethodAsync(value);
    }

    private System.Threading.CancellationTokenSource? _cariSearchCts;

    partial void OnCariSearchTextChanged(string value)
    {
        _cariSearchCts?.Cancel();
        _cariSearchCts = new System.Threading.CancellationTokenSource();
        var token = _cariSearchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                InvokeOnUIThreadAsync(async () => await LoadCariListAsync());
            }
        }, token);
    }

    private async Task LoadCariListAsync()
    {
        try
        {
            var all = await _uow.Cariler.GetAllAsync();
            var filtered = all.Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(CariSearchText))
            {
                filtered = filtered.Where(c =>
                    (c.Unvan ?? "").Contains(CariSearchText, StringComparison.OrdinalIgnoreCase) ||
                    (c.CariKod ?? "").Contains(CariSearchText, StringComparison.OrdinalIgnoreCase));
            }

            await InvokeOnUIThreadAsync(() =>
            {
                SearchedCariler = new ObservableCollection<CariKart>(filtered.Take(50));
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cari search error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void SelectQuickCari(CariKart? cari)
    {
        if (cari != null)
        {
            SelectedQuickCari = cari;
            IsCariSearchVisible = false;
        }
    }

    [RelayCommand]
    public void ClearSelectedCari()
    {
        SelectedQuickCari = null;
    }

    private async Task LoadAccountsForMethodAsync(string method)
    {
        try
        {
            var all = await _uow.Bankalar.GetAllAsync();
            List<BankaKart> filtered;

            if (method == "Nakit")
                filtered = all.Where(x => x.KartTuru == "Kasa").ToList();
            else
                filtered = all.Where(x => x.KartTuru != "Kasa").ToList();

            if (!filtered.Any()) filtered = all;

            await InvokeOnUIThreadAsync(() =>
            {
                AvailableAccounts = new ObservableCollection<BankaKart>(filtered);
                SelectedAccount = AvailableAccounts.FirstOrDefault();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Account load error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveQuickTransactionAsync()
    {
        if (IsSaving) return;

        // Validation
        if (SelectedQuickCari == null)
        {
            ErrorMessage = "Lütfen bir cari seçin!";
            return;
        }
        if (QuickAmount <= 0)
        {
            ErrorMessage = "İşlem tutarı 0'dan büyük olmalıdır!";
            return;
        }
        if (SelectedAccount == null)
        {
            ErrorMessage = "Lütfen bir hesap seçin!";
            return;
        }

        IsSaving = true;
        try
        {
            var tarih = DateTime.TryParseExact(QuickDateStr, "dd.MM.yyyy", null,
                System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.Now;
            
            string methodAbbr = QuickPaymentMethod switch
            {
                "Kredi Kartı" => "KK",
                "Havale / EFT" => "EFT",
                _ => "Nakit"
            };
            string prefix = QuickPaymentMethod switch
            {
                "Kredi Kartı" => "KK-",
                "Havale / EFT" => "EFT-",
                _ => "TS-"
            };
            var evrakNo = prefix + DateTime.Now.ToString("yyMMddHHmmss");

            // 1. Create CariHareket
            var hareket = new CariHareket
            {
                CariId = SelectedQuickCari.Id,
                CariUnvan = SelectedQuickCari.Unvan,
                Tarih = tarih,
                Aciklama = $"[{QuickPaymentMethod}] {QuickDescription}".Trim(),
                IslemTuru = $"{QuickTransactionType} ({methodAbbr})",
                EvrakNo = evrakNo,
                Alacak = QuickTransactionType == "Tahsilat" ? QuickAmount : 0,
                Borc = QuickTransactionType == "Ödeme" ? QuickAmount : 0
            };
            await _uow.Cariler.SaveHareketAsync(hareket);

            // 2. Create Kasa/Banka Hareket
            if (QuickPaymentMethod == "Nakit")
            {
                var kasaHareket = new KasaHareket
                {
                    KasaId = SelectedAccount.Id,
                    Tarih = tarih,
                    Aciklama = $"{SelectedQuickCari.Unvan} - {QuickTransactionType} ({QuickDescription})",
                    EvrakNo = evrakNo,
                    IslemTuru = $"{QuickTransactionType} ({methodAbbr})",
                    Giren = QuickTransactionType == "Tahsilat" ? QuickAmount : 0,
                    Cikan = QuickTransactionType == "Ödeme" ? QuickAmount : 0,
                    CariUnvan = SelectedQuickCari.Unvan
                };
                await _uow.Kasalar.SaveAsync(kasaHareket);
                SelectedAccount.GuncelBakiye += (kasaHareket.Giren - kasaHareket.Cikan);
                await _uow.Bankalar.SaveAsync(SelectedAccount);
            }
            else
            {
                var bankaHareket = new BankaHareket
                {
                    BankaId = SelectedAccount.Id,
                    BankaAdi = SelectedAccount.BankaAdi,
                    IBAN = SelectedAccount.IBAN,
                    Tarih = tarih,
                    Aciklama = $"{SelectedQuickCari.Unvan} - {QuickTransactionType} ({QuickDescription})",
                    EvrakNo = evrakNo,
                    IslemTuru = $"{QuickTransactionType} ({methodAbbr})",
                    Giren = QuickTransactionType == "Tahsilat" ? QuickAmount : 0,
                    Cikan = QuickTransactionType == "Ödeme" ? QuickAmount : 0,
                    Tutar = QuickAmount,
                    CariUnvan = SelectedQuickCari.Unvan
                };
                await _uow.Bankalar.SaveHareketAsync(bankaHareket);
                SelectedAccount.GuncelBakiye += (bankaHareket.Giren - bankaHareket.Cikan);
                await _uow.Bankalar.SaveAsync(SelectedAccount);
            }

            // 3. Recalculate Cari Balance
            await _uow.Cariler.RecalculateBalanceAsync(SelectedQuickCari.Id);

            // 4. Refresh Dashboard
            IsQuickTransactionVisible = false;
            ResetQuickTransaction();
            await LoadFinansStatsAsync();
            SuccessMessage = "İşlem başarıyla kaydedildi!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"İşlem kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}

