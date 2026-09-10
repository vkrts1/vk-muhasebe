using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests;

public class CariModuleHeadlessTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task Create_New_Cari_Via_Dialog_Saves_To_Database_And_List()
    {
        // 1. Arrange
        var cariVm = _serviceProvider.GetRequiredService<CariListViewModel>();
        await cariVm.LoadCarilerAsync();
        int initialCount = cariVm.Cariler.Count;

        // 2. Act: Son kullanıcı gibi yeni cari diyalog butonuna basar
        cariVm.OpenAddCariDialog();
        Assert.True(cariVm.IsCariEkleVisible);

        // Form alanlarını doldurur
        cariVm.EditUnvan = "Ermay Test Müşterisi A.Ş.";
        cariVm.EditGrup = "Müşteri";
        cariVm.EditVergiNo = "1234567890";
        cariVm.EditTelefon = "05551234567";
        cariVm.EditIl = "Bursa";

        // Kaydet butonuna tıklar
        await cariVm.SaveCariCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.False(cariVm.IsCariEkleVisible, "Diyalog kapanmış olmalı.");
        Assert.True(string.IsNullOrEmpty(cariVm.ErrorMessage), $"Hata mesajı olmamalı: {cariVm.ErrorMessage}");

        // Veritabanı ve ViewModel listesi kontrolü
        var tumCariler = await _uow.Cariler.GetAllAsync();
        var eklenen = tumCariler.FirstOrDefault(c => c.Unvan == "Ermay Test Müşterisi A.Ş.");

        Assert.NotNull(eklenen);
        Assert.Equal("1234567890", eklenen.VergiNo);
        Assert.Equal("Bursa", eklenen.Il);
        Assert.Equal(initialCount + 1, cariVm.Cariler.Count);
    }

    [AvaloniaFact]
    public async Task Search_Filter_Finds_Matching_Cari_Kart()
    {
        // 1. Arrange: İki farklı cari ekle
        var c1 = new CariKart { Unvan = "Alfa Otomotiv Sanayi", Grup = "Müşteri" };
        var c2 = new CariKart { Unvan = "Beta Lojistik Hizmetleri", Grup = "Tedarikçi" };
        await _uow.Cariler.SaveAsync(c1);
        await _uow.Cariler.SaveAsync(c2);

        var cariVm = _serviceProvider.GetRequiredService<CariListViewModel>();

        // 2. Act: "Alfa" araması yap
        cariVm.SearchString = "Alfa";
        await cariVm.LoadCarilerAsync();

        // 3. Assert: Sadece Alfa bulunmalı
        Assert.Contains(cariVm.Cariler, c => c.Unvan == "Alfa Otomotiv Sanayi");
        Assert.DoesNotContain(cariVm.Cariler, c => c.Unvan == "Beta Lojistik Hizmetleri");
    }

    [AvaloniaFact]
    public async Task Tahsilat_Transaction_Updates_Cari_Balance_Correctly()
    {
        // 1. Arrange: 5000 TL borcu olan bir müşteri ekle
        var cari = new CariKart
        {
            Unvan = "Bakiye Test Müşterisi",
            Grup = "Müşteri",
            Borc = 5000,
            Alacak = 0
        };
        await _uow.Cariler.SaveAsync(cari);
        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cari.Id,
            CariUnvan = cari.Unvan,
            Tarih = DateTime.Now,
            IslemTuru = "Açılış",
            Aciklama = "Açılış Bakiyesi",
            Borc = 5000,
            Alacak = 0
        });

        // Varsayılan kasa ekle
        var kasa = new BankaKart { BankaAdi = "Merkez Nakit Kasa", Bakiye = 1000, KartTuru = "Kasa" };
        await _uow.Bankalar.SaveAsync(kasa);

        var cariVm = _serviceProvider.GetRequiredService<CariListViewModel>();
        await cariVm.LoadCarilerAsync();
        cariVm.SelectedCari = cariVm.Cariler.First(c => c.Id == cari.Id);

        // 2. Act: 2000 TL Tahsilat gir
        cariVm.OpenTransactionDialog("Tahsilat");
        Assert.True(cariVm.IsTransactionDialogVisible);

        cariVm.TransactionAmount = 2000;
        cariVm.TransactionMethod = "Nakit";
        cariVm.SelectedKasaForTransaction = kasa;
        cariVm.SelectedKasaId = kasa.Id;
        cariVm.TransactionDescription = "Test cari tahsilatı";

        await cariVm.SaveTransactionCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.False(cariVm.IsTransactionDialogVisible);

        // Carinin güncel bakiyesi (Borç: 5000, Alacak: 2000 => Net Kalan: 3000)
        var guncelCari = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.NotNull(guncelCari);
        Assert.Equal(5000, guncelCari.Borc);
        Assert.Equal(2000, guncelCari.Alacak);
        Assert.Equal(3000, guncelCari.Bakiye);
    }
}
