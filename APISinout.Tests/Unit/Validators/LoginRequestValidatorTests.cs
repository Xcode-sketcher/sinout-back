// ============================================================
// 🔓 TESTES DO LOGINREQUESTVALIDATOR - VALIDAÇÃO DE LOGIN
// ============================================================
// Valida as regras de validação para login de usuários,
// incluindo formato de email e requisitos de senha.

using Xunit;
using FluentAssertions;
using APISinout.Validators;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Unit.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange - Configura requisição de login válida
        var request = UserFixtures.CreateValidLoginRequest();

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyEmail_ShouldFail()
    {
        // Arrange - Configura requisição com email vazio
        var request = UserFixtures.CreateValidLoginRequest();
        request.Email = "";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_ShouldFail()
    {
        // Arrange - Configura requisição com email inválido
        var request = UserFixtures.CreateValidLoginRequest();
        request.Email = "invalid-email";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WithEmptyPassword_ShouldFail()
    {
        // Arrange - Configura requisição com senha vazia
        var request = UserFixtures.CreateValidLoginRequest();
        request.Password = "";

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
