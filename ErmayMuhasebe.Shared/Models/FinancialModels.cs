using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Models
{
    public class DashboardStats
    {
        public decimal GunlukCiro { get; set; }
        public decimal AylikCiro { get; set; }
        public int KritikStokSayisi { get; set; }
        public decimal ToplamBorc { get; set; }
        public decimal ToplamAlacak { get; set; }
        public decimal ToplamTahsilat { get; set; }
        public decimal ToplamNakitVarligi { get; set; } // Kasa + Banka
        public decimal BugunOdenecek { get; set; }
        public decimal BugunTahsilat { get; set; }
        public decimal BekleyenOdeme { get; set; }
        public decimal ToplamMaliyet { get; set; }
        
        // Advanced Analytics
        public double StokDevirHizi { get; set; } // Stock Turnover
        public int CariTahsilatSuresi { get; set; } // Days Sales Outstanding (DSO)
        public decimal Karlilik { get; set; }
        public double KarlilikOrani { get; set; }
    }

    public class RecentTransactionItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = "In"; // In, Out, Neutral
        public string TextColor { get; set; } = "#10B981"; // Default Green
    }

    public class CariAlertItem
    {
        public int CariId { get; set; }
        public string Unvan { get; set; } = "";
        public decimal Bakiye { get; set; }
        public DateTime? SonIslemTarihi { get; set; }
        public int OrtalamaVade { get; set; }
    }

    public class IncomeExpenseItem
    {
        public string Month { get; set; } = "";
        public int Year { get; set; }
        public int MonthInt { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    public class OdemePlanView
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }
        public string? Tur { get; set; }
        public decimal Tutar { get; set; }
        public string? Durum { get; set; }
        public string? Aciklama { get; set; }
        public string? Yon { get; set; } 
        public string? SourceType { get; set; } 
    }
    
    public class FinancialSummary 
    {
        public string? Type { get; set; } 
        public decimal TotalCash { get; set; } 
        public decimal TotalBank { get; set; } 
        public decimal TotalReceivable { get; set; } 
        public decimal TotalPayable { get; set; } 
        public decimal TotalCollection { get; set; } 
        public decimal TotalPayment { get; set; } 
        public decimal Net { get; set; } 
        
        public decimal CreditCardDebt { get; set; } 
        public decimal CustomerChecksInHand { get; set; } 
    }

    public class FinancialReportData 
    { 
        public DateTime StartDate { get; set; } 
        public DateTime EndDate { get; set; } 
        public List<FinancialSummary> Items { get; set; } = new(); 
    }
    
    // --- Nested Helper Classes for Expense Report ---
    public class CategoryStat 
    { 
        public string Category { get; set; } = "";
        public decimal Amount { get; set; } 
        public double Percentage { get; set; } 
    }
    public class ChannelStat 
    { 
        public string Channel { get; set; } = "";
        public decimal Amount { get; set; } 
    }
    public class CardStat 
    { 
        public string CardName { get; set; } = "";
        public decimal Amount { get; set; } 
    }
    public class ExpenseItemView 
    { 
        public string Description { get; set; } = "";
        public string Channel { get; set; } = "";
        public decimal Amount { get; set; } 
    }
    public class DailyLedgerItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public string Channel { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class ExpenseReportData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalExpenses { get; set; }
        public List<KasaHareket> Expenses { get; set; } = new();
        
        public decimal TotalExpenseThisMonth { get; set; }
        public decimal NetProfitLoss { get; set; }
        public decimal MonthlyChangePercent { get; set; }
        
        public List<CategoryStat> Categories { get; set; } = new(); 
        public List<ChannelStat> Channels { get; set; } = new();
        public List<CardStat> CardExpenses { get; set; } = new();
        public List<ExpenseItemView> TopExpenses { get; set; } = new();
        public List<DailyLedgerItem> DailyLedger { get; set; } = new();
        
        public decimal TotalExpense { get; set; } 
    }
    
    public class StokHareketView : StokHareket 
    {
        public decimal Giris { get; set; }
        public decimal Cikis { get; set; }
        // EvrakTuru and Birim are inherited from StokHareket now.
    }

    public class CityProfitStat
    {
        public string Sehir { get; set; } = "";
        public decimal SatisToplam { get; set; }
        public decimal AlisToplam { get; set; }
        public decimal Profit => SatisToplam - AlisToplam;
    }

    public class CariSummary
    {
        public decimal ToplamBorc { get; set; }
        public decimal ToplamAlacak { get; set; }
        public decimal MusteriBakiye { get; set; }
        public decimal TedarikciBakiye { get; set; }
        public int ToplamCari { get; set; }
    }

    public class VadeReportItem
    {
        public string Tur { get; set; } = "";
        public string CariAdi { get; set; } = "";
        public DateTime VadeTarihi { get; set; }
        public decimal Tutar { get; set; }
        public string Durum { get; set; } = "";
    }

    public class FinanceTrendItem
    {
        public string Label { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal Income { get; set; }        // Gelen Tahsilat (Nakit/Banka)
        public decimal Redirected { get; set; }    // Yönlendirilen Tahsilat (Çek/Senet Ciro vb.)
        public decimal Expense { get; set; }       // Ödemeler
        public decimal Net => Income + Redirected - Expense;
    }
}
