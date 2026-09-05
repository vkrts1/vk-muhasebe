using ErmayMuhasebe.Models;
using Xunit;

namespace ErmayMuhasebe.Tests.Models;

/// <summary>
/// CariKart modeli için unit testler
/// Cari hesaplamalarını ve özelliklerini test eder
/// </summary>
public class CariKartTests
{
    [Fact]
    public void CariKart_Bakiye_ShouldCalculateCorrectly()
    {
        // Arrange
        var cari = new CariKart
        {
            Borc = 5000m,
            Alacak = 2000m
        };
        
        // Act
        decimal bakiye = cari.Bakiye;
        
        // Assert
        Assert.Equal(3000m, bakiye); // 5000 - 2000 = 3000
    }

    [Fact]
    public void CariKart_Bakiye_NegativeBakiye_ShouldBeNegative()
    {
        // Arrange
        var cari = new CariKart
        {
            Borc = 1000m,
            Alacak = 5000m
        };
        
        // Act
        decimal bakiye = cari.Bakiye;
        
        // Assert
        Assert.Equal(-4000m, bakiye); // 1000 - 5000 = -4000
    }

    [Fact]
    public void CariKart_Bakiye_Zero_ShouldBeZero()
    {
        // Arrange
        var cari = new CariKart
        {
            Borc = 1000m,
            Alacak = 1000m
        };
        
        // Act
        decimal bakiye = cari.Bakiye;
        
        // Assert
        Assert.Equal(0m, bakiye); // Eşit olduğunda 0 olmalı
    }

    [Fact]
    public void CariKart_VKN_ShouldEqualVergiNo()
    {
        // Arrange
        var cari = new CariKart
        {
            VergiNo = "1234567890"
        };
        
        // Act
        string? vkn = cari.VKN;
        
        // Assert
        Assert.NotNull(vkn);
        Assert.Equal("1234567890", vkn);
        Assert.Equal(cari.VergiNo, vkn); // VKN ve VergiNo aynı olmalı
    }

    [Fact]
    public void CariKart_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var cari = new CariKart();
        
        // Assert
        Assert.True(cari.AktifMi); // Aktif olmalı
        Assert.True(cari.KayitTarihi <= DateTime.Now); // Kayıt tarihi bugün veya geçmiş olmalı
    }
}

