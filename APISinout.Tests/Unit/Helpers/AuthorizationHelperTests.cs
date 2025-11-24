// ============================================================
// 🔐 TESTES DO AUTHORIZATIONHELPER - SEGURANÇA DE CLAIMS
// ============================================================
// Valida que a extração de claims do JWT funciona corretamente
// e trata casos de erro apropriadamente.

using System.Security.Claims;
using Xunit;
using FluentAssertions;
using APISinout.Helpers;
using APISinout.Models;

namespace APISinout.Tests.Unit.Helpers;

public class AuthorizationHelperTests
{
    [Fact]
    public void GetCurrentUserId_ValidUserId_ReturnsUserId()
    {
        // Arrange - Configura claims válidas com userId
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act - Executa extração do userId
        var result = AuthorizationHelper.GetCurrentUserId(claimsPrincipal);

        // Assert - Verifica se retornou o userId correto
        result.Should().Be("123");
    }

    [Fact]
    public void GetCurrentUserId_ValidNameIdentifier_ReturnsUserId()
    {
        // Arrange - Usando ClaimTypes.NameIdentifier como fallback
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "456"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = AuthorizationHelper.GetCurrentUserId(claimsPrincipal);

        // Assert
        result.Should().Be("456");
    }

    [Fact]
    public void GetCurrentUserId_NoUserIdClaim_ThrowsAppException()
    {
        // Arrange - Configura claims sem userId
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert - Verifica se lança exceção
        var action = () => AuthorizationHelper.GetCurrentUserId(claimsPrincipal);
        action.Should().Throw<AppException>()
            .WithMessage("*Usuário não encontrado*");
    }

    // Removido GetCurrentUserId_InvalidUserId_ThrowsAppException pois userId agora é string e qualquer string é válida

    [Fact]
    public void GetCurrentUserRole_ValidRole_ReturnsRole()
    {
        // Arrange - Configura claims com role válida
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act - Executa extração da role
        var result = AuthorizationHelper.GetCurrentUserRole(claimsPrincipal);

        // Assert - Verifica se retornou a role correta
        result.Should().Be("Cuidador");
    }

    [Fact]
    public void GetCurrentUserRole_NoRoleClaim_ThrowsAppException()
    {
        // Arrange - Configura claims sem role
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert - Verifica se lança exceção
        var action = () => AuthorizationHelper.GetCurrentUserRole(claimsPrincipal);
        action.Should().Throw<AppException>()
            .WithMessage("*Role não encontrada*");
    }

    [Fact]
    public void GetCurrentUserEmail_ValidEmail_ReturnsEmail()
    {
        // Arrange - Configura claims com email válido
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act - Executa extração do email
        var result = AuthorizationHelper.GetCurrentUserEmail(claimsPrincipal);

        // Assert - Verifica se retornou o email correto
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void GetCurrentUserEmail_NoEmailClaim_ReturnsNull()
    {
        // Arrange - Configura claims sem email
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act - Executa extração do email
        var result = AuthorizationHelper.GetCurrentUserEmail(claimsPrincipal);

        // Assert - Verifica se retornou null
        result.Should().BeNull();
    }

    [Fact]
    public void IsCuidador_CuidadorRole_ReturnsTrue()
    {
        // Arrange - Configura claims com role Cuidador
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Role, UserRole.Cuidador.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act - Executa verificação se é Cuidador
        var result = AuthorizationHelper.IsCuidador(claimsPrincipal);

        // Assert - Verifica se retornou true
        result.Should().BeTrue();
    }

    // Removido IsCuidador_AdminRole_ReturnsFalse pois role Admin foi descontinuada

    [Fact]
    public void GetCurrentUserId_EmptyClaims_ThrowsAppException()
    {
        // Arrange - Configura claims vazias
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert - Verifica se lança exceção
        var action = () => AuthorizationHelper.GetCurrentUserId(claimsPrincipal);
        action.Should().Throw<AppException>();
    }
}
