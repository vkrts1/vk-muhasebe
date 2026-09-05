using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;
using System.IO;
using Xunit;

namespace ErmayMuhasebe.Tests.Repositories;

/// <summary>
/// StokRepository için unit testler
/// </summary>
[Collection("DatabaseTests")] // Paralel çalışmayı engelle
public class StokRepositoryTests : IDisposable
{
    private readonly DatabaseService _dbService;
    private readonly StokRepository _repository;
    private readonly string _testDbPath;

    public StokRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid()}.db3");
        ErmayMuhasebe.Data.Constants.DatabasePath = _testDbPath;
        
        _dbService = new DatabaseService();
        _repository = new StokRepository(_dbService);
    }

    [Fact]
    public async Task SaveAsync_NewStok_ShouldReturnId()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var stok = new StokKart
        {
            StokKodu = "STK001",
            StokAdi = "Test Ürün",
            SatisFiyati = 100.50m
        };
        
        // Act
        int id = await _repository.SaveAsync(stok);
        
        // Assert
        Assert.True(id > 0);
        Assert.Equal(id, stok.Id);
    }

    [Fact]
    public async Task GetByKodAsync_ExistingStok_ShouldReturnStok()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var stok = new StokKart
        {
            StokKodu = "STK001",
            StokAdi = "Test Ürün"
        };
        await _repository.SaveAsync(stok);
        
        // Act
        var result = await _repository.GetByKodAsync("STK001");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("STK001", result.StokKodu);
        Assert.Equal("Test Ürün", result.StokAdi);
    }

    [Fact]
    public async Task GetByBarkodAsync_ExistingStok_ShouldReturnStok()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var stok = new StokKart
        {
            StokKodu = "STK001",
            StokAdi = "Test Ürün",
            Barkod = "1234567890123"
        };
        await _repository.SaveAsync(stok);
        
        // Act
        var result = await _repository.GetByBarkodAsync("1234567890123");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("1234567890123", result.Barkod);
    }

    [Fact]
    public async Task GetByKategoriAsync_ShouldFilterByKategori()
    {
        // Arrange
        await _dbService.InitializeAsync();
        await _repository.SaveAsync(new StokKart { StokAdi = "Ürün 1", Kategori = "Elektronik" });
        await _repository.SaveAsync(new StokKart { StokAdi = "Ürün 2", Kategori = "Gıda" });
        await _repository.SaveAsync(new StokKart { StokAdi = "Ürün 3", Kategori = "Elektronik" });
        
        // Act
        var elektronik = await _repository.GetByKategoriAsync("Elektronik");
        
        // Assert
        Assert.NotNull(elektronik);
        Assert.Equal(2, elektronik.Count);
        Assert.All(elektronik, s => Assert.Equal("Elektronik", s.Kategori));
    }

    [Fact]
    public async Task GetKritikStoklarAsync_ShouldReturnOnlyCritical()
    {
        // Arrange
        await _dbService.InitializeAsync();
        await _repository.SaveAsync(new StokKart { StokAdi = "Kritik 1", Miktar = 5, MinSeviye = 10 });
        await _repository.SaveAsync(new StokKart { StokAdi = "Normal 1", Miktar = 50, MinSeviye = 10 });
        await _repository.SaveAsync(new StokKart { StokAdi = "Kritik 2", Miktar = 8, MinSeviye = 10 });
        
        // Act
        var kritik = await _repository.GetKritikStoklarAsync();
        
        // Assert
        Assert.NotNull(kritik);
        Assert.Equal(2, kritik.Count);
        Assert.All(kritik, s => Assert.True(s.Miktar <= s.MinSeviye));
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCascade()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var stok = new StokKart
        {
            StokKodu = "STK001",
            StokAdi = "Silinecek Ürün"
        };
        int id = await _repository.SaveAsync(stok);
        
        // Act
        await _repository.DeleteAsync(id);
        
        // Assert
        var result = await _repository.GetByIdAsync(id);
        Assert.Null(result);
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

