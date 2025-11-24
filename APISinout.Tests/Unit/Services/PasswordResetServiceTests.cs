// ============================================================
// 🔑 TESTES DO PASSWORDRESETSERVICE - RESET DE SENHA
// ============================================================
// Valida a lógica de reset de senha, geração de tokens,
// validação de tokens e alteração de senha com segurança.

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
        // Arrange - Configura usuário válido e mocks para solicitação de reset de senha
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act - Executa a solicitação de reset de senha
        var result = await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert - Verifica se o token foi criado e o email enviado
        result.Should().NotBeNull();
        result.Message.Should().Contain("receberá um código");
        _mockResetRepository.Verify(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(user.Email!, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithEmptyEmail_ShouldThrowException()
    {
        // Arrange - Configura requisição com email vazio
        var request = new ForgotPasswordRequest { Email = "" };

        // Act - Tenta solicitar reset com email vazio
        var act = async () => await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert - Verifica se lança exceção de email obrigatório
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Email é obrigatório");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonExistentEmail_ShouldReturnSuccessWithoutSendingEmail()
    {
        // Arrange - Configura email inexistente no sistema
        var request = PasswordResetFixtures.CreateForgotPasswordRequest("nonexistent@test.com");
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);

        // Act - Executa a solicitação com email inexistente
        var result = await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert - Verifica se retorna sucesso mas não envia email
        result.Message.Should().Contain("receberá um código");
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenRateLimited_ShouldThrowException()
    {
        // Arrange - Configura rate limiting ativo para o email
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(true);

        // Act - Tenta solicitar reset quando limitado
        var act = async () => await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert - Verifica se lança exceção de muitas tentativas
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Muitas tentativas*");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ShouldGenerateNumericCodeWith6Digits()
    {
        // Arrange - Configura mocks para capturar token gerado
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        var user = UserFixtures.CreateValidUser();
        PasswordResetToken? capturedToken = null;
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>()))
            .Callback<PasswordResetToken>(token => capturedToken = token)
            .Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act - Executa a solicitação
        await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert - Verifica se o token gerado tem 6 dígitos numéricos
        capturedToken.Should().NotBeNull();
        capturedToken!.Token.Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenEmailSendingFails_ShouldReturnDevMessage()
    {
        // Arrange - Configura falha no envio de email
        var request = PasswordResetFixtures.CreateForgotPasswordRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email server error"));

        // Act - Executa a solicitação com falha no email
        var result = await _passwordResetService.RequestPasswordResetAsync(request);

        // Assert - Verifica se retorna mensagem de desenvolvimento e loga erro
        result.Message.Should().Contain("Código de redefinição (DEV)");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro ao enviar email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange - Configura token válido e usuário para reset de senha
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        var token = PasswordResetFixtures.CreateValidToken();
        var user = UserFixtures.CreateValidUser();
        
        _mockResetRepository.Setup(x => x.GetByTokenAsync(request.Token)).ReturnsAsync(token);
        _mockUserRepository.Setup(x => x.GetByIdAsync(token.UserId!)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockResetRepository.Setup(x => x.MarkAsUsedAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act - Executa o reset de senha
        var result = await _passwordResetService.ResetPasswordAsync(request);

        // Assert - Verifica se a senha foi atualizada e o token marcado como usado
        result.Message.Should().Be("Senha redefinida com sucesso");
        _mockUserRepository.Verify(x => x.UpdateUserAsync(user.Id!, It.IsAny<User>()), Times.Once);
        _mockResetRepository.Verify(x => x.MarkAsUsedAsync(token.Id!), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowException()
    {
        // Arrange - Configura token inválido para teste
        var request = PasswordResetFixtures.CreateResetPasswordRequest("invalid-token");
        
        _mockResetRepository.Setup(x => x.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((PasswordResetToken?)null);

        // Act - Tenta resetar senha com token inválido
        var act = async () => await _passwordResetService.ResetPasswordAsync(request);

        // Assert - Verifica se lança exceção de token inválido
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Token inválido ou expirado");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMismatchedPasswords_ShouldThrowException()
    {
        // Arrange - Configura senhas não coincidentes
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        request.ConfirmPassword = "DifferentPassword123";

        // Act - Tenta resetar senha com confirmação incorreta
        var act = async () => await _passwordResetService.ResetPasswordAsync(request);

        // Assert - Verifica se lança exceção de senhas não coincidentes
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senhas não coincidem");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithWeakPassword_ShouldThrowException()
    {
        // Arrange - Configura senha fraca para teste de validação
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        request.NewPassword = "123";
        request.ConfirmPassword = "123";

        // Act - Tenta resetar senha com senha fraca
        var act = async () => await _passwordResetService.ResetPasswordAsync(request);

        // Assert - Verifica se lança exceção de senha fraca
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senha deve ter no mínimo 6 caracteres");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldSendPasswordChangedNotification()
    {
        // Arrange - Configura mocks para testar envio de notificação de mudança de senha
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        var token = PasswordResetFixtures.CreateValidToken();
        var user = UserFixtures.CreateValidUser();
        
        _mockResetRepository.Setup(x => x.GetByTokenAsync(request.Token)).ReturnsAsync(token);
        _mockUserRepository.Setup(x => x.GetByIdAsync(token.UserId!)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockResetRepository.Setup(x => x.MarkAsUsedAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act - Executa o reset de senha
        await _passwordResetService.ResetPasswordAsync(request);

        // Assert - Verifica se a notificação foi enviada
        _mockEmailService.Verify(x => x.SendPasswordChangedNotificationAsync(user.Email!), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenNotificationFails_ShouldNotThrow()
    {
        // Arrange - Configura falha no envio de notificação
        var request = PasswordResetFixtures.CreateResetPasswordRequest();
        var token = PasswordResetFixtures.CreateValidToken();
        var user = UserFixtures.CreateValidUser();
        
        _mockResetRepository.Setup(x => x.GetByTokenAsync(request.Token)).ReturnsAsync(token);
        _mockUserRepository.Setup(x => x.GetByIdAsync(token.UserId!)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockResetRepository.Setup(x => x.MarkAsUsedAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email server error"));

        // Act - Executa o reset de senha com falha na notificação
        var result = await _passwordResetService.ResetPasswordAsync(request);

        // Assert - Verifica se o processo completa mesmo com erro na notificação
        result.Message.Should().Be("Senha redefinida com sucesso");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro ao enviar notificação")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ShouldChangePassword()
    {
        // Arrange - Configura dados válidos para mudança de senha
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        var user = UserFixtures.CreateValidUser();
        var userId = user.Id;
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId!)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act - Executa a alteração de senha
        var result = await _passwordResetService.ChangePasswordAsync(request, userId!);

        // Assert - Verifica se a senha foi alterada com sucesso
        result.Message.Should().Be("Senha alterada com sucesso");
        _mockUserRepository.Verify(x => x.UpdateUserAsync(userId!, It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ShouldThrowException()
    {
        // Arrange - Configura senha atual incorreta para teste
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        request.CurrentPassword = "WrongPassword123";
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.Id!)).ReturnsAsync(user);

        // Act - Tenta alterar senha com senha atual incorreta
        var act = async () => await _passwordResetService.ChangePasswordAsync(request, user.Id!);

        // Assert - Verifica se lança exceção de senha incorreta
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senha atual incorreta");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMismatchedNewPasswords_ShouldThrowException()
    {
        // Arrange - Configura senhas novas não coincidentes
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        request.ConfirmPassword = "DifferentPassword123";

        // Act - Tenta alterar senha com confirmação incorreta
        var act = async () => await _passwordResetService.ChangePasswordAsync(request, MongoDB.Bson.ObjectId.GenerateNewId().ToString());

        // Assert - Verifica se lança exceção de senhas não coincidentes
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Senhas não coincidem");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldSendNotificationEmail()
    {
        // Arrange - Configura mocks para testar envio de notificação de mudança de senha
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.Id!)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act - Executa a alteração de senha
        await _passwordResetService.ChangePasswordAsync(request, user.Id!);

        // Assert - Verifica se a notificação foi enviada
        _mockEmailService.Verify(x => x.SendPasswordChangedNotificationAsync(user.Email!), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenNotificationFails_ShouldNotThrow()
    {
        // Arrange - Configura falha no envio de notificação
        var request = PasswordResetFixtures.CreateChangePasswordRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(user.Id!)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email server error"));

        // Act - Executa a alteração de senha com falha na notificação
        var result = await _passwordResetService.ChangePasswordAsync(request, user.Id!);

        // Assert - Verifica se o processo completa mesmo com erro na notificação
        result.Message.Should().Be("Senha alterada com sucesso");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro ao enviar notificação")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ResendResetCode Tests

    [Fact]
    public async Task ResendResetCodeAsync_WithValidEmail_ShouldCreateNewToken()
    {
        // Arrange - Configura email válido para reenvio de código
        var request = new ResendResetCodeRequest { Email = "test@test.com" };
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.GetActiveTokenByUserIdAsync(user.Id!)).ReturnsAsync((PasswordResetToken?)null);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act - Executa o reenvio do código
        var result = await _passwordResetService.ResendResetCodeAsync(request);

        // Assert - Verifica se um novo token foi criado
        result.Message.Should().Contain("reenviado");
        _mockResetRepository.Verify(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendResetCodeAsync_TooSoon_ShouldThrowException()
    {
        // Arrange - Configura token recente para testar limite de tempo
        var request = new ResendResetCodeRequest { Email = "test@test.com" };
        var user = UserFixtures.CreateValidUser();
        var recentToken = PasswordResetFixtures.CreateValidToken();
        recentToken.CreatedAt = DateTime.UtcNow.AddMinutes(-2); // 2 minutos atrás
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.GetActiveTokenByUserIdAsync(user.Id!)).ReturnsAsync(recentToken);

        // Act - Tenta reenviar código antes do tempo permitido
        var act = async () => await _passwordResetService.ResendResetCodeAsync(request);

        // Assert - Verifica se lança exceção de tempo de espera
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Aguarde*");
    }

    [Fact]
    public async Task ResendResetCodeAsync_WhenEmailSendingFails_ShouldReturnDevMessage()
    {
        // Arrange - Configura falha no envio de email
        var request = new ResendResetCodeRequest { Email = "test@test.com" };
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockRateLimitService.Setup(x => x.IsRateLimited(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _mockResetRepository.Setup(x => x.GetActiveTokenByUserIdAsync(user.Id!)).ReturnsAsync((PasswordResetToken?)null);
        _mockResetRepository.Setup(x => x.CreateTokenAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
        _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email server error"));

        // Act - Executa o reenvio com falha no email
        var result = await _passwordResetService.ResendResetCodeAsync(request);

        // Assert - Verifica se retorna mensagem de desenvolvimento e loga erro
        result.Message.Should().Contain("Código de redefinição (DEV)");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro ao reenviar email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
