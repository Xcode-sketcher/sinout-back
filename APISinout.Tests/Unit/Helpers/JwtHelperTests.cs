// ============================================================
// 🎫 TESTES DO JWTHELPER - GERAÇÃO E VALIDAÇÃO DE TOKENS
// ============================================================
// Valida a geração de tokens JWT, inclusão de claims,
// assinatura e validação de tokens.

using Xunit;
using FluentAssertions;
using APISinout.Helpers;
using APISinout.Models;
using APISinout.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace APISinout.Tests.Unit.Helpers;

/// <summary>
/// Testes para JwtHelper - Geração e validação de tokens
/// </summary>
public class JwtHelperTests
{
    private readonly IConfiguration _configuration;

    public JwtHelperTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Key", "TestJwtKeyForUnitTestingPurposesOnlyNotForProductionUse123456789"},
            {"Jwt:Issuer", "SinoutAPI"},
            {"Jwt:Audience", "SinoutClient"},
            {"Jwt:AccessTokenExpirationMinutes", "60"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    [Fact]
    public void GenerateToken_WithValidUser_ShouldReturnValidJwtToken()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();

        // Act
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Should().NotBeNull();
        jwtToken.Issuer.Should().Be("SinoutAPI");
        jwtToken.Audiences.Should().Contain("SinoutClient");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeUserIdClaim()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser("123");

        // Act
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be("123");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeEmailClaim()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();

        // Act
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(user.Email);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeRoleClaim()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();

        // Act
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(user.Role);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectExpiration()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var expectedExpiration = beforeGeneration.AddMinutes(60);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }

    // Removido GenerateToken_ForAdminUser_ShouldIncludeAdminRole pois role Admin foi descontinuada

    [Fact]
    public void GenerateToken_ShouldBeValidJwtFormat()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();

        // Act
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        
        // Token JWT deve ter 3 partes separadas por ponto
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_WithDifferentUsers_ShouldGenerateDifferentTokens()
    {
        // Arrange
        var user1 = UserFixtures.CreateValidUser("1");
        var user2 = UserFixtures.CreateValidUser("2");

        // Act
        var token1 = JwtHelper.GenerateToken(user1, _configuration);
        var token2 = JwtHelper.GenerateToken(user2, _configuration);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_WithNullEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();
        user.Email = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Email do usuário não pode ser null ou vazio");
    }

    [Fact]
    public void GenerateToken_WithEmptyEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();
        user.Email = string.Empty;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Email do usuário não pode ser null ou vazio");
    }

    [Fact]
    public void GenerateToken_WithNullRole_ShouldThrowArgumentException()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();
        user.Role = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Role do usuário não pode ser null ou vazio");
    }

    [Fact]
    public void GenerateToken_WithEmptyRole_ShouldThrowArgumentException()
    {
        // Arrange
        var user = UserFixtures.CreateValidUser();
        user.Role = string.Empty;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Role do usuário não pode ser null ou vazio");
    }
}
