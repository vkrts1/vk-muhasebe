using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;
using System.IO;
using System.Linq;
using Xunit;

namespace ErmayMuhasebe.Tests.Repositories;

/// <summary>
/// CariRepository için unit testler
/// Repository pattern'in doğru çalıştığını test eder
/// </summary>
[Collection("DatabaseTests")] // Paralel çalışmayı engelle
public class CariRepositoryTests : IDisposable
{
    private readonly DatabaseService _dbService;
    private readonly CariRepository _repository;
    private readonly string _testDbPath;

    public CariRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid()}.db3");
        ErmayMuhasebe.Data.Constants.DatabasePath = _testDbPath;
        
        _dbService = new DatabaseService();
        _repository = new CariRepository(_dbService);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoData()
    {
        // Arrange
        await _dbService.InitializeAsync();
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_NewCari_ShouldReturnId()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var cari = new CariKart
        {
            Unvan = "Test Cari A.Ş.",
            Tur = "Alici"
        };
        
        // Act
        int id = await _repository.SaveAsync(cari);
        
        // Assert
        Assert.True(id > 0);
        Assert.Equal(id, cari.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCari_ShouldReturnCari()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var cari = new CariKart
        {
            Unvan = "Test Cari A.Ş.",
            Tur = "Alici"
        };
        int id = await _repository.SaveAsync(cari);
        
        // Act
        var result = await _repository.GetByIdAsync(id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Cari A.Ş.", result.Unvan);
        Assert.Equal("Alici", result.Tur);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingCari_ShouldReturnNull()
    {
        // Arrange
        await _dbService.InitializeAsync();
        
        // Act
        var result = await _repository.GetByIdAsync(99999);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_UpdateExisting_ShouldUpdate()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var cari = new CariKart
        {
            Unvan = "Test Cari A.Ş.",
            Tur = "Alici"
        };
        int id = await _repository.SaveAsync(cari);
        
        // Act
        cari.Unvan = "Güncellenmiş Cari A.Ş.";
        await _repository.SaveAsync(cari);
        
        // Assert
        var updated = await _repository.GetByIdAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("Güncellenmiş Cari A.Ş.", updated.Unvan);
    }

    [Fact]
    public async Task DeleteAsync_ShouldMarkAsDeleted()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var cari = new CariKart
        {
            Unvan = "Silinecek Cari",
            Tur = "Alici"
        };
        int id = await _repository.SaveAsync(cari);
        
        // Act
        await _repository.DeleteAsync(cari);
        
        // Assert
        var deleted = await _repository.GetDeletedAsync();
        Assert.Contains(deleted, c => c.Id == id && c.IsDeleted);
        
        // Silinen cari GetAllAsync'de görünmemeli
        var all = await _repository.GetAllAsync();
        Assert.DoesNotContain(all, c => c.Id == id);
    }

    [Fact]
    public async Task RestoreAsync_ShouldUnmarkAsDeleted()
    {
        // Arrange
        await _dbService.InitializeAsync();
        var cari = new CariKart
        {
            Unvan = "Geri Yüklenecek Cari",
            Tur = "Alici"
        };
        int id = await _repository.SaveAsync(cari);
        await _repository.DeleteAsync(cari);
        
        // Act - GetDeletedAsync kullan çünkü GetByIdAsync silinen kayıtları getirmiyor
        var deletedCariler = await _repository.GetDeletedAsync();
        var deletedCari = deletedCariler.FirstOrDefault(c => c.Id == id);
        Assert.NotNull(deletedCari); // Silinen cari bulunmalı
        
        await _repository.RestoreAsync(deletedCari);
        
        // Assert
        var restored = await _repository.GetByIdAsync(id);
        Assert.NotNull(restored);
        Assert.False(restored.IsDeleted);
    }

    [Fact]
    public async Task GetByTurAsync_ShouldFilterByTur()
    {
        // Arrange
        await _dbService.InitializeAsync();
        await _repository.SaveAsync(new CariKart { Unvan = "Alici 1", Tur = "Alici" });
        await _repository.SaveAsync(new CariKart { Unvan = "Satici 1", Tur = "Satici" });
        await _repository.SaveAsync(new CariKart { Unvan = "Alici 2", Tur = "Alici" });
        
        // Act
        var alicilar = await _repository.GetByTurAsync("Alici");
        
        // Assert
        Assert.NotNull(alicilar);
        Assert.Equal(2, alicilar.Count);
        Assert.All(alicilar, c => Assert.Equal("Alici", c.Tur));
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

