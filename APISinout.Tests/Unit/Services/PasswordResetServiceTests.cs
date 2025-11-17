// ============================================================
// 🔑 TESTES DO PASSWORDRESETSERVICE - RECUPERAÇÃO DE SENHA
// ============================================================
// Valida o fluxo completo de recuperação de senha:
// solicitação, validação de código, reset e alteração de senha.

using Xunit;
using Moq;
using FluentAssertions;
using APISinout.Services;
using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using APISinout.Tests.Fixtures;
using Microsoft.Extensions.Logging;

namespace APISinout.Tests.Unit.Services;

/// <summary>
/// Testes completos para PasswordResetService
/// Cobertura: Solicitação, reenvio, reset e mudança de senha
/// </summary>
public class PasswordResetServiceTests
{
    private readonly Mock<IPasswordResetRepository> _mockResetRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IRateLimitService> _mockRateLimitService;
    private readonly Mock<ILogger<PasswordResetService>> _mockLogger;
    private readonly PasswordResetService _passwordResetService;

    public PasswordResetServiceTests()
    {
        _mockResetRepository = new Mock<IPasswordResetRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockRateLimitService = new Mock<IRateLimitService>();
        _mockLogger = new Mock<ILogger<PasswordResetService>>();

        _passwordResetService = new PasswordResetService(
            _mockResetRepository.Object,
            _mockUserRepository.Object,
            _mockEmailService.Object,
            _mockRateLimitService.Object,
            _mockLogger.Object
        );
    }

    #region RequestPasswordReset Tests

    [Fact]
    public async Task RequestPasswordResetAsync_WithValidEmail_ShouldCreateTokenAndSendEmail()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Contain("receberá um código");
        _mockResetRepository.Verify(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(user.Email!, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithEmptyEmail_ShouldThrowException()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "" };

        // Act
        var act = async () => await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Email é obrigatório");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonExistentEmail_ShouldReturnSuccessWithoutSendingEmail()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateForgotPasswordRequest("nonexistent@test.com");
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);

        // Act
        var result = await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert
        result.Message.Should().Contain("receberá um código");
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithInactiveUser_ShouldThrowException()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        var user = UserFixtures.CreateInactiveUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);

        // Act
        var act = async () => await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Usuário inativo");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenRateLimited_ShouldThrowException()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(true);

        // Act
        var act = async () => await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Muitas tentativas*");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ShouldGenerateNumericCodeWith6Digits()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        var user = UserFixtures.CreateValidUser();
        PasswordResetToken? capturedToken = null;
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>()))
            .Callback<PasswordResetToken>(token => capturedToken = token)
            .Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert
        capturedToken.Should().NotBeNull();
        capturedToken!.Token.Should().MatchRegex("^[0-9]{6}$");
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        var token = PasswordResetFixtures.CreateValidToken();
        var user = UserFixtures.CreateValidUser();
        
        _mockResetRepository.Setup(x => x.GetByTokenAsync(request.Token)).ReturnsAsync(token);
        _mockUserRepository.Setup(x => x.GetByIdAsync(token.UserId)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<int>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockResetRepository.Setup(x => x.MarkAsUsedAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.ResetPasswordAsync(request);

        // Assert
        result.Message.Should().Be("Senha redefinida com sucesso");
        _mockUserRepository.Verify(x => x.UpdateUserAsync(user.UserId, It.IsAny<User>()), Times.Once);
        _mockResetRepository.Verify(x => x.MarkAsUsedAsync(token.Id!), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowException()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateResetPasswordRequest("invalid-token");
        
        _mockResetRepository.Setup(x => x.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((PasswordResetToken?)null);

        // Act
        var act = async () => await _passwordResetService.ResetPasswordAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Token inválido ou expirado");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMismatchedPasswords_ShouldThrowException()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        request.ConfirmPassword = "DifferentPassword123";

        // Act
        var act = async () => await _passwordResetService.ResetPasswordAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senhas não coincidem");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithWeakPassword_ShouldThrowException()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        request.NewPassword = "123";
        request.ConfirmPassword = "123";

        // Act
        var act = async () => await _passwordResetService.ResetPasswordAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senha deve ter no mínimo 6 caracteres");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldSendPasswordChangedNotification()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        var token = PasswordResetFixtures.CreateValidToken();
        var user = UserFixtures.CreateValidUser();
        
        _mockResetRepository.Setup(x => x.GetByTokenAsync(request.Token)).ReturnsAsync(token);
        _mockUserRepository.Setup(x => x.GetByIdAsync(token.UserId)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<int>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockResetRepository.Setup(x => x.MarkAsUsedAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _passwordResetService.ResetPasswordAsync(request);

        // Assert
        _mockEmailService.Verify(x => x.SendPasswordChangedNotificationAsync(user.Email!), Times.Once);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ShouldChangePassword()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        var user = UserFixtures.CreateValidUser();
        var userId = user.UserId;
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<int>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.ChangePasswordAsync(request, userId);

        // Assert
        result.Message.Should().Be("Senha alterada com sucesso");
        _mockUserRepository.Verify(x => x.UpdateUserAsync(userId, It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ShouldThrowException()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        request.CurrentPassword = "WrongPassword123";
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

        // Act
        var act = async () => await _passwordResetService.ChangePasswordAsync(request, user.UserId);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senha atual incorreta");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMismatchedNewPasswords_ShouldThrowException()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        request.ConfirmPassword = "DifferentPassword123";

        // Act
        var act = async () => await _passwordResetService.ChangePasswordAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senhas não coincidem");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldSendNotificationEmail()
    {
        // Arrange
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<int>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _passwordResetService.ChangePasswordAsync(request, user.UserId);

        // Assert
        _mockEmailService.Verify(x => x.SendPasswordChangedNotificationAsync(user.Email!), Times.Once);
    }

    #endregion

    #region ResendResetCode Tests

    [Fact]
    public async Task ResendResetCodeAsync_WithValidEmail_ShouldCreateNewToken()
    {
        // Arrange
        var request = new ResendResetCodeRequest { Email = "test@test.com" };
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.GetActiveTokenByUserIdAsync(user.UserId)).ReturnsAsync((PasswordResetToken?)null);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.ResendResetCodeAsync(request);

        // Assert
        result.Message.Should().Contain("reenviado");
        _mockResetRepository.Verify(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendResetCodeAsync_TooSoon_ShouldThrowException()
    {
        // Arrange
        var request = new ResendResetCodeRequest { Email = "test@test.com" };
        var user = UserFixtures.CreateValidUser();
        var recentToken = PasswordResetFixtures.CreateValidToken();
        recentToken.CreatedAt = DateTime.UtcNow.AddMinutes(-2); // 2 minutos atrás
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.GetActiveTokenByUserIdAsync(user.UserId)).ReturnsAsync(recentToken);

        // Act
        var act = async () => await _passwordResetService.ResendResetCodeAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Aguarde*");
    }

    #endregion
}
