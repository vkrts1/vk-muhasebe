using ErmayMuhasebe.Models;
using Xunit;

namespace ErmayMuhasebe.Tests.Models;

/// <summary>
/// Fatura modeli için unit testler
/// Fatura hesaplamalarını ve özelliklerini test eder
/// </summary>
public class FaturaTests
{
    [Fact]
    public void Fatura_Kalan_ShouldCalculateCorrectly()
    {
        // Arrange
        var fatura = new Fatura
        {
            GenelToplam = 1000m,
            Odenen = 300m
        };
        
        // Act
        decimal kalan = fatura.Kalan;
        
        // Assert
        Assert.Equal(700m, kalan); // 1000 - 300 = 700
    }

    [Fact]
    public void Fatura_Bakiye_ShouldEqualKalan()
    {
        // Arrange
        var fatura = new Fatura
        {
            GenelToplam = 1000m,
            Odenen = 300m
        };
        
        // Act
        decimal kalan = fatura.Kalan;
        decimal bakiye = fatura.Bakiye;
        
        // Assert
        Assert.Equal(kalan, bakiye); // Bakiye ve Kalan aynı olmalı
    }

    [Fact]
    public void Fatura_Kalan_FullyPaid_ShouldBeZero()
    {
        // Arrange
        var fatura = new Fatura
        {
            GenelToplam = 1000m,
            Odenen = 1000m
        };
        
        // Act
        decimal kalan = fatura.Kalan;
        
        // Assert
        Assert.Equal(0m, kalan); // Tam ödendiğinde kalan 0 olmalı
    }

    [Fact]
    public void Fatura_KdvToplam_ShouldEqualToplamKDV()
    {
        // Arrange
        var fatura = new Fatura
        {
            ToplamKDV = 200m
        };
        
        // Act
        decimal kdvToplam = fatura.KdvToplam;
        
        // Assert
        Assert.Equal(200m, kdvToplam);
        Assert.Equal(fatura.ToplamKDV, kdvToplam);
    }

    [Fact]
    public void Fatura_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var fatura = new Fatura();
        
        // Assert
        Assert.NotNull(fatura.Detaylar); // Detaylar listesi oluşturulmuş olmalı
        Assert.False(fatura.IptalMi); // İptal değil olmalı
        Assert.True(fatura.Tarih <= DateTime.Now); // Tarih bugün veya geçmiş olmalı
    }
}

