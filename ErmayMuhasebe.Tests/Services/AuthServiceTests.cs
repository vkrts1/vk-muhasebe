using ErmayMuhasebe.Services;
using Xunit;

namespace ErmayMuhasebe.Tests.Services;

/// <summary>
/// AuthService için unit testler
/// Şifre hashleme ve doğrulama işlemlerini test eder
/// </summary>
public class AuthServiceTests
{
    [Fact]
    public void HashPassword_ShouldReturnHashedString()
    {
        // Arrange (Hazırlık)
        string password = "Test123!";
        string salt = AuthService.GenerateSalt();
        
        // Act (İşlem)
        string hashed = AuthService.HashPassword(password, salt);
        
        // Assert (Doğrulama)
        Assert.NotNull(hashed);
        Assert.NotEmpty(hashed);
        Assert.NotEqual(password, hashed); 
    }

    [Fact]
    public void HashPassword_SamePasswordAndSalt_ShouldReturnSameHash()
    {
        // Arrange
        string password = "Test123!";
        string salt = AuthService.GenerateSalt();
        
        // Act
        string hash1 = AuthService.HashPassword(password, salt);
        string hash2 = AuthService.HashPassword(password, salt);
        
        // Assert
        Assert.Equal(hash1, hash2); 
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        string password = "Test123!";
        string salt = AuthService.GenerateSalt();
        string hash = AuthService.HashPassword(password, salt);
        
        // Act
        bool result = AuthService.VerifyPassword(password, hash, salt);
        
        // Assert
        Assert.True(result); 
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ShouldReturnFalse()
    {
        // Arrange
        string correctPassword = "Test123!";
        string wrongPassword = "WrongPassword";
        string salt = AuthService.GenerateSalt();
        string hash = AuthService.HashPassword(correctPassword, salt);
        
        // Act
        bool result = AuthService.VerifyPassword(wrongPassword, hash, salt);
        
        // Assert
        Assert.False(result); 
    }

    [Fact]
    public void HashPassword_DifferentSalts_ShouldReturnDifferentHashes()
    {
        // Arrange
        string password = "Test123!";
        string salt1 = AuthService.GenerateSalt();
        string salt2 = AuthService.GenerateSalt();
        
        // Act
        string hash1 = AuthService.HashPassword(password, salt1);
        string hash2 = AuthService.HashPassword(password, salt2);
        
        // Assert
        Assert.NotEqual(hash1, hash2); 
    }
}

