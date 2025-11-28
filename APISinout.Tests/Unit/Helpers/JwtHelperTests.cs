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
        // Arrange - Prepara dados e configuração para geração de token (usuário)
        var user = UserFixtures.CreateValidUser();

        // Act - Executa a geração do token
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert - Verifica o token JWT gerado
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
        // Arrange - Prepara dados para gerar token com userId definido
        var user = UserFixtures.CreateValidUser("123");

        // Act - Executa a geração do token
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert - Verifica se o claim userId está presente
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be("123");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeEmailClaim()
    {
        // Arrange - Prepara dados para gerar token com email definido
        var user = UserFixtures.CreateValidUser();

        // Act - Executa a geração do token
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert - Verifica se o claim email está presente
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(user.Email);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeRoleClaim()
    {
        // Arrange - Prepara dados para gerar token com role definido
        var user = UserFixtures.CreateValidUser();

        // Act - Executa a geração do token
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert - Verifica se o claim role está presente
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(user.Role);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectExpiration()
    {
        // Arrange - Prepara dados e captura timestamp antes da geração do token
        var user = UserFixtures.CreateValidUser();
        var beforeGeneration = DateTime.UtcNow;

        // Act - Executa a geração do token
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert - Verifica se a expiração do token está correta
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var expectedExpiration = beforeGeneration.AddMinutes(60);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }

    // Removido GenerateToken_ForAdminUser_ShouldIncludeAdminRole pois role Admin foi descontinuada

    [Fact]
    public void GenerateToken_ShouldBeValidJwtFormat()
    {
        // Arrange - Prepara dados para gerar token e validar formato JWT
        var user = UserFixtures.CreateValidUser();

        // Act - Executa a geração do token
        var token = JwtHelper.GenerateToken(user, _configuration);

        // Assert - Verifica o formato JWT do token gerado
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        
        // Token JWT deve ter 3 partes separadas por ponto
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_WithDifferentUsers_ShouldGenerateDifferentTokens()
    {
        // Arrange - Prepara dois usuários distintos para comparação de tokens
        var user1 = UserFixtures.CreateValidUser("1");
        var user2 = UserFixtures.CreateValidUser("2");

        // Act - Gera tokens para usuários distintos
        var token1 = JwtHelper.GenerateToken(user1, _configuration);
        var token2 = JwtHelper.GenerateToken(user2, _configuration);

        // Assert - Verifica que tokens gerados são diferentes
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_WithNullEmail_ShouldThrowArgumentException()
    {
        // Arrange - Prepara usuário com email null para testar exceção
        var user = UserFixtures.CreateValidUser();
        user.Email = null!;

        // Act & Assert - Executa a ação e verifica exceção para email null
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Email do usuário não pode ser null ou vazio");
    }

    [Fact]
    public void GenerateToken_WithEmptyEmail_ShouldThrowArgumentException()
    {
        // Arrange - Prepara usuário com email vazio para testar exceção
        var user = UserFixtures.CreateValidUser();
        user.Email = string.Empty;

        // Act & Assert - Executa a ação e verifica exceção para email vazio
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Email do usuário não pode ser null ou vazio");
    }

    [Fact]
    public void GenerateToken_WithNullRole_ShouldThrowArgumentException()
    {
        // Arrange - Prepara usuário com role null para testar exceção
        var user = UserFixtures.CreateValidUser();
        user.Role = null!;

        // Act & Assert - Executa a ação e verifica exceção para role null
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Role do usuário não pode ser null ou vazio");
    }

    [Fact]
    public void GenerateToken_WithEmptyRole_ShouldThrowArgumentException()
    {
        // Arrange - Prepara usuário com role vazio para testar exceção
        var user = UserFixtures.CreateValidUser();
        user.Role = string.Empty;

        // Act & Assert - Executa a ação e verifica exceção para role vazio
        var exception = Assert.Throws<ArgumentException>(() => JwtHelper.GenerateToken(user, _configuration));
        exception.Message.Should().Contain("Role do usuário não pode ser null ou vazio");
    }
}
