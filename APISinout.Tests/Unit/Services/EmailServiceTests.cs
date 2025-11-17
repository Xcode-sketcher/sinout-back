// ============================================================
// 📧 TESTES DO EMAILSERVICE - ENVIO DE EMAILS
// ============================================================
// Valida o envio de emails de recuperação de senha,
// notificações de alteração de senha e logs em modo desenvolvimento.

using Xunit;
using Moq;
using FluentAssertions;
using APISinout.Services;
using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using APISinout.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace APISinout.Tests.Unit.Services;

public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailService>>();

        // Configure email settings for tests
        _mockConfiguration.Setup(x => x["Email:SmtpServer"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(x => x["Email:SmtpPort"]).Returns("587");
        _mockConfiguration.Setup(x => x["Email:Username"]).Returns("");
        _mockConfiguration.Setup(x => x["Email:Password"]).Returns("");
        _mockConfiguration.Setup(x => x["Email:FromEmail"]).Returns("test@test.com");
        _mockConfiguration.Setup(x => x["Email:FromName"]).Returns("Test App");

        _emailService = new EmailService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithoutCredentials_ShouldLogAndReturnWithoutError()
    {
        // Arrange
        var toEmail = "user@test.com";
        var resetCode = "123456";

        // Act - Should not throw
        await _emailService.SendPasswordResetEmailAsync(toEmail, resetCode);

        // Assert - Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MODO DEV")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordChangedNotificationAsync_WithoutCredentials_ShouldNotThrowException()
    {
        // Arrange
        var toEmail = "user@test.com";

        // Act
        Func<Task> act = async () => await _emailService.SendPasswordChangedNotificationAsync(toEmail);

        // Assert - Should not throw
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithValidEmail_ShouldLogInformation()
    {
        // Arrange
        var toEmail = "user@test.com";
        var resetCode = "123456";

        // Act
        await _emailService.SendPasswordResetEmailAsync(toEmail, resetCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Preparando email de reset")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void EmailService_Constructor_ShouldLoadConfigurationCorrectly()
    {
        // Arrange & Act - Created in constructor
        
        // Assert - Verify configuration was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configurado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldLogResetCodeInDevMode()
    {
        // Arrange
        var toEmail = "user@test.com";
        var resetCode = "123456";

        // Act
        await _emailService.SendPasswordResetEmailAsync(toEmail, resetCode);

        // Assert - Verify code was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(resetCode)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
