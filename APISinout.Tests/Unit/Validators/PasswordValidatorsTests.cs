// ============================================================
// 🔑 TESTES DOS PASSWORD VALIDATORS - VALIDAÇÃO DE SENHAS
// ============================================================
// Valida as regras de validação para forgot-password,
// reset-password e change-password.

using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using APISinout.Models;
using APISinout.Validators;

namespace APISinout.Tests.Unit.Validators;

public class PasswordValidatorsTests
{
    #region ForgotPasswordRequestValidator Tests

    [Fact]
    public void ForgotPasswordRequestValidator_ValidEmail_PassesValidation()
    {
        // Arrange - Configura email válido para teste de validação
        var validator = new ForgotPasswordRequestValidator();
        var request = new ForgotPasswordRequest { Email = "test@example.com" };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ForgotPasswordRequestValidator_EmptyEmail_FailsValidation()
    {
        // Arrange - Configura email vazio para teste de falha de validação
        var validator = new ForgotPasswordRequestValidator();
        var request = new ForgotPasswordRequest { Email = "" };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email é obrigatório");
    }

    [Fact]
    public void ForgotPasswordRequestValidator_InvalidEmail_FailsValidation()
    {
        // Arrange - Configura email inválido para teste de falha de validação
        var validator = new ForgotPasswordRequestValidator();
        var request = new ForgotPasswordRequest { Email = "invalid-email" };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email inválido");
    }

    #endregion

    #region ResetPasswordRequestValidator Tests

    [Fact]
    public void ResetPasswordRequestValidator_ValidRequest_PassesValidation()
    {
        // Arrange - Configura requisição válida para reset de senha
        var validator = new ResetPasswordRequestValidator();
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ResetPasswordRequestValidator_EmptyToken_FailsValidation()
    {
        // Arrange - Configura token vazio para teste de falha de validação
        var validator = new ResetPasswordRequestValidator();
        var request = new ResetPasswordRequest
        {
            Token = "",
            NewPassword = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token)
            .WithErrorMessage("Token é obrigatório");
    }

    [Fact]
    public void ResetPasswordRequestValidator_ShortPassword_FailsValidation()
    {
        // Arrange - Configura senha muito curta para teste de falha de validação
        var validator = new ResetPasswordRequestValidator();
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "123",
            ConfirmPassword = "123"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Senha deve ter no mínimo 6 caracteres");
    }

    [Fact]
    public void ResetPasswordRequestValidator_NoUppercase_FailsValidation()
    {
        // Arrange - Configura senha sem maiúscula para teste de falha de validação
        var validator = new ResetPasswordRequestValidator();
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "password123!",
            ConfirmPassword = "password123!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Senha deve conter pelo menos uma letra maiúscula");
    }

    [Fact]
    public void ResetPasswordRequestValidator_NoLowercase_FailsValidation()
    {
        // Arrange - Configura senha sem minúscula para teste de falha de validação
        var validator = new ResetPasswordRequestValidator();
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "PASSWORD123!",
            ConfirmPassword = "PASSWORD123!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Senha deve conter pelo menos uma letra minúscula");
    }

    [Fact]
    public void ResetPasswordRequestValidator_NoNumber_FailsValidation()
    {
        // Arrange - Configura senha sem número para teste de falha de validação
        var validator = new ResetPasswordRequestValidator();
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "Password!",
            ConfirmPassword = "Password!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Senha deve conter pelo menos um número");
    }

    [Fact]
    public void ResetPasswordRequestValidator_TooLongPassword_FailsValidation()
    {
        // Arrange - Configura senha muito longa para teste de falha de validação
        var validator = new ResetPasswordRequestValidator();
        var longPassword = new string('a', 101) + "A1!"; // 104 caracteres
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = longPassword,
            ConfirmPassword = longPassword
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Senha não pode ter mais de 100 caracteres");
    }

    #endregion

    #region ChangePasswordRequestValidator Tests

    [Fact]
    public void ChangePasswordRequestValidator_ValidRequest_PassesValidation()
    {
        // Arrange - Configura requisição válida para mudança de senha
        var validator = new ChangePasswordRequestValidator();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChangePasswordRequestValidator_EmptyCurrentPassword_FailsValidation()
    {
        // Arrange - Configura senha atual vazia para teste de falha de validação
        var validator = new ChangePasswordRequestValidator();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword)
            .WithErrorMessage("Senha atual é obrigatória");
    }

    [Fact]
    public void ChangePasswordRequestValidator_WeakNewPassword_FailsValidation()
    {
        // Arrange - Configura nova senha fraca para teste de falha de validação
        var validator = new ChangePasswordRequestValidator();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "weak",
            ConfirmPassword = "weak"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ChangePasswordRequestValidator_PasswordsDoNotMatch_FailsValidation()
    {
        // Arrange - Configura senhas diferentes para teste de falha de validação
        var validator = new ChangePasswordRequestValidator();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Senhas não coincidem");
    }

    [Fact]
    public void ChangePasswordRequestValidator_AllPasswordRules_FailsValidation()
    {
        // Arrange - Configura senha que viola todas as regras para teste de falha de validação
        var validator = new ChangePasswordRequestValidator();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "abc", // Muito curta, sem maiúscula, sem número, sem especial
            ConfirmPassword = "abc"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        errors.Should().Contain("Senha deve ter no mínimo 6 caracteres");
        errors.Should().Contain("Senha deve conter pelo menos uma letra maiúscula");
        errors.Should().Contain("Senha deve conter pelo menos um número");
    }

    #endregion
}
