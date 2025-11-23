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
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = AuthorizationHelper.GetCurrentUserId(claimsPrincipal);

        // Assert
        result.Should().Be("123");
    }

    [Fact]
    public void GetCurrentUserId_ValidNameIdentifier_ReturnsUserId()
    {
        // Arrange - usando ClaimTypes.NameIdentifier como fallback
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
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert
        var action = () => AuthorizationHelper.GetCurrentUserId(claimsPrincipal);
        action.Should().Throw<AppException>()
            .WithMessage("*Usuário não encontrado*");
    }

    // Removed GetCurrentUserId_InvalidUserId_ThrowsAppException as userId is now string and any string is valid

    [Fact]
    public void GetCurrentUserRole_ValidRole_ReturnsRole()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = AuthorizationHelper.GetCurrentUserRole(claimsPrincipal);

        // Assert
        result.Should().Be("Cuidador");
    }

    [Fact]
    public void GetCurrentUserRole_NoRoleClaim_ThrowsAppException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert
        var action = () => AuthorizationHelper.GetCurrentUserRole(claimsPrincipal);
        action.Should().Throw<AppException>()
            .WithMessage("*Role não encontrada*");
    }

    [Fact]
    public void GetCurrentUserEmail_ValidEmail_ReturnsEmail()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = AuthorizationHelper.GetCurrentUserEmail(claimsPrincipal);

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void GetCurrentUserEmail_NoEmailClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Role, "Cuidador")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = AuthorizationHelper.GetCurrentUserEmail(claimsPrincipal);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsCuidador_CuidadorRole_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Role, UserRole.Cuidador.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = AuthorizationHelper.IsCuidador(claimsPrincipal);

        // Assert
        result.Should().BeTrue();
    }

    // Removed IsCuidador_AdminRole_ReturnsFalse as Admin role is deprecated

    [Fact]
    public void GetCurrentUserId_EmptyClaims_ThrowsAppException()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act & Assert
        var action = () => AuthorizationHelper.GetCurrentUserId(claimsPrincipal);
        action.Should().Throw<AppException>();
    }
}
