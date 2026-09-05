using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;
using System.IO;
using Xunit;

namespace ErmayMuhasebe.Tests.Repositories;

/// <summary>
/// FaturaRepository için unit testler
/// </summary>
[Collection("DatabaseTests")] // Paralel çalışmayı engelle
public class FaturaRepositoryTests : IDisposable
{
    private readonly DatabaseService _dbService;
    private readonly FaturaRepository _repository;
    private readonly string _testDbPath;

    public FaturaRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid()}.db3");
        ErmayMuhasebe.Data.Constants.DatabasePath = _testDbPath;
        
        _dbService = new DatabaseService();
        _repository = new FaturaRepository(_dbService);
    }

    [Fact]
    public async Task SaveAsync_NewFatura_ShouldReturnId()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var fatura = new Fatura
        {
            FaturaNo = "FAT001",
            Tur = "Satış",
            GenelToplam = 1000m
        };
        
        // Act
        int id = await _repository.SaveAsync(fatura);
        
        // Assert
        Assert.True(id > 0);
        Assert.Equal(id, fatura.Id);
    }

    [Fact]
    public async Task GetByNoAsync_ExistingFatura_ShouldReturnFatura()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var fatura = new Fatura
        {
            FaturaNo = "FAT001",
            Tur = "Satış"
        };
        await _repository.SaveAsync(fatura);
        
        // Act
        var result = await _repository.GetByNoAsync("FAT001");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("FAT001", result.FaturaNo);
    }

    [Fact]
    public async Task GetByCariIdAsync_ShouldFilterByCari()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var fatura1 = new Fatura { FaturaNo = "FAT001", CariId = 1, Tur = "Satış" };
        var fatura2 = new Fatura { FaturaNo = "FAT002", CariId = 2, Tur = "Satış" };
        var fatura3 = new Fatura { FaturaNo = "FAT003", CariId = 1, Tur = "Satış" };
        
        await _repository.SaveAsync(fatura1);
        await _repository.SaveAsync(fatura2);
        await _repository.SaveAsync(fatura3);
        
        // Act
        var cari1Faturalar = await _repository.GetByCariIdAsync(1);
        
        // Assert
        Assert.NotNull(cari1Faturalar);
        Assert.Equal(2, cari1Faturalar.Count);
        Assert.All(cari1Faturalar, f => Assert.Equal(1, f.CariId));
    }

    [Fact]
    public async Task GetByTurAsync_ShouldFilterByTur()
    {
        // Arrange
        await _dbService.InitializeAsync();
        await _repository.SaveAsync(new Fatura { FaturaNo = "FAT001", Tur = "Satış" });
        await _repository.SaveAsync(new Fatura { FaturaNo = "FAT002", Tur = "Alış" });
        await _repository.SaveAsync(new Fatura { FaturaNo = "FAT003", Tur = "Satış" });
        
        // Act
        var satisFaturalar = await _repository.GetByTurAsync("Satış");
        
        // Assert
        Assert.NotNull(satisFaturalar);
        Assert.Equal(2, satisFaturalar.Count);
        Assert.All(satisFaturalar, f => Assert.Equal("Satış", f.Tur));
    }

    [Fact]
    public async Task SaveWithDetailsAsync_ShouldSaveFaturaAndDetails()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var fatura = new Fatura
        {
            FaturaNo = "FAT001",
            Tur = "Satış",
            GenelToplam = 1200m
        };
        var detaylar = new List<FaturaDetay>
        {
            new FaturaDetay { StokAdi = "Ürün 1", Miktar = 2, BirimFiyat = 500m, ToplamTutar = 1000m },
            new FaturaDetay { StokAdi = "Ürün 2", Miktar = 1, BirimFiyat = 200m, ToplamTutar = 200m }
        };
        
        // Act
        int result = await _repository.SaveWithDetailsAsync(fatura, detaylar);
        
        // Assert
        Assert.Equal(1, result);
        Assert.True(fatura.Id > 0);
        
        var savedDetaylar = await _repository.GetDetaylarAsync(fatura.Id);
        Assert.NotNull(savedDetaylar);
        Assert.Equal(2, savedDetaylar.Count);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch { }
    }
}

