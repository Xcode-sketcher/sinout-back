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

        
        _mockConfiguration.Setup(x => x["Email:SmtpServer"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(x => x["Email:SmtpPort"]).Returns("587");
        _mockConfiguration.Setup(x => x["Email:Username"]).Returns("");
        _mockConfiguration.Setup(x => x["Email:Password"]).Returns("");
        _mockConfiguration.Setup(x => x["Email:FromEmail"]).Returns("test@test.com");
        _mockConfiguration.Setup(x => x["Email:FromName"]).Returns("Test App");

        _emailService = new EmailService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact(Skip = "Teste de envio de email real - requer SMTP configurado")]
    [Trait("Category", "ExternalEmail")]
    public async Task SendPasswordResetEmailAsync_WithoutCredentials_ShouldLogAndReturnWithoutError()
    {
        // Arrange - Configura email e código de reset
        var toEmail = "user@test.com";
        var resetCode = "123456";

        // Act - Executa método que deve funcionar sem erro
        Func<Task> act = async () => await _emailService.SendPasswordResetEmailAsync(toEmail, resetCode);

        // Assert - Não deve lançar exceção
        await act.Should().NotThrowAsync();
        
        
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
        // Arrange - Configura email de destino
        var toEmail = "user@test.com";

        // Act - Executa notificação de mudança de senha
        Func<Task> act = async () => await _emailService.SendPasswordChangedNotificationAsync(toEmail);

        // Assert - Não deve lançar exceção
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Teste de envio de email real - requer SMTP configurado")]
    [Trait("Category", "ExternalEmail")]
    public async Task SendPasswordResetEmailAsync_WithValidEmail_ShouldLogInformation()
    {
        // Arrange - Configura email e código de reset
        var toEmail = "user@test.com";
        var resetCode = "123456";

        // Act - Executa envio que não deve lançar erro
        Func<Task> act = async () => await _emailService.SendPasswordResetEmailAsync(toEmail, resetCode);
        
        // Assert - Não deve lançar erro
        await act.Should().NotThrowAsync();
        
        // Verifica se log de informação foi chamado
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
        // Arrange & Act - Serviço criado no construtor da classe
        
        // Assert - Verifica se configuração foi logada
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
    public async Task SendPasswordResetEmailAsync_InDevMode_ShouldLogResetCode()
    {
        // Arrange - Cria serviço com credenciais vazias para forçar modo dev
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<EmailService>>();
        
        // Limpa variáveis de ambiente para forçar modo dev
        Environment.SetEnvironmentVariable("EMAIL__SMTPSERVER", null);
        Environment.SetEnvironmentVariable("EMAIL__SMTPPORT", null);
        Environment.SetEnvironmentVariable("EMAIL__USERNAME", null);
        Environment.SetEnvironmentVariable("EMAIL__PASSWORD", null);
        Environment.SetEnvironmentVariable("EMAIL__FROMEMAIL", null);
        
        // Configura credenciais vazias na configuração
        mockConfig.Setup(x => x["Email:SmtpServer"]).Returns("smtp.gmail.com");
        mockConfig.Setup(x => x["Email:SmtpPort"]).Returns("587");
        mockConfig.Setup(x => x["Email:Username"]).Returns((string?)null);
        mockConfig.Setup(x => x["Email:Password"]).Returns((string?)null);
        mockConfig.Setup(x => x["Email:FromEmail"]).Returns((string?)null);
        mockConfig.Setup(x => x["Email:FromName"]).Returns("Test App");

        var service = new EmailService(mockConfig.Object, mockLogger.Object);
        var toEmail = "user@test.com";
        var resetCode = "ABC123";

        // Act - Executa envio de email
        await service.SendPasswordResetEmailAsync(toEmail, resetCode);

        // Assert - Deve logar o código de reset no modo dev (sem credenciais)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Código de redefinição: ABC123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GeneratePasswordResetEmailBody_ShouldContainResetCode()
    {
        // Arrange - Define código de reset
        var resetCode = "ABC123";
        
        // Usa reflexão para acessar método interno
        var method = typeof(EmailService).GetMethod("GeneratePasswordResetEmailBody", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act - Invoca método interno
        var body = method?.Invoke(_emailService, new object[] { resetCode }) as string;

        // Assert - Verifica conteúdo do corpo do email
        body.Should().NotBeNull();
        body.Should().Contain(resetCode);
        body.Should().Contain("Redefinição de Senha");
        body.Should().Contain("Sinout");
        body.Should().Contain("expira em 1 hora");
        body.Should().Contain("<!DOCTYPE html>");
    }

    [Fact]
    public void GeneratePasswordChangedEmailBody_ShouldContainSuccessMessage()
    {
        // Arrange & Act - Usa reflexão para acessar método interno
        var method = typeof(EmailService).GetMethod("GeneratePasswordChangedEmailBody", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var body = method?.Invoke(_emailService, null) as string;

        // Assert - Verifica conteúdo da mensagem de sucesso
        body.Should().NotBeNull();
        body.Should().Contain("Senha Alterada com Sucesso");
        body.Should().Contain("Sua senha foi alterada com sucesso");
        body.Should().Contain("notificação de segurança");
        body.Should().Contain("<!DOCTYPE html>");
    }

    [Fact]
    public void EmailService_Constructor_WithEmptyCredentials_ShouldLogNotConfigured()
    {
        // Arrange - Cria mocks para configuração e logger
        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<EmailService>>();
        
        // Limpa variáveis de ambiente primeiro
        Environment.SetEnvironmentVariable("EMAIL__SMTPSERVER", null);
        Environment.SetEnvironmentVariable("EMAIL__SMTPPORT", null);
        Environment.SetEnvironmentVariable("EMAIL__USERNAME", null);
        Environment.SetEnvironmentVariable("EMAIL__PASSWORD", null);
        Environment.SetEnvironmentVariable("EMAIL__FROMEMAIL", null);
        
        // Configura credenciais vazias - sobrescreve todas as configurações de email
        mockConfig.Setup(x => x["Email:SmtpServer"]).Returns("smtp.gmail.com");
        mockConfig.Setup(x => x["Email:SmtpPort"]).Returns("587");
        mockConfig.Setup(x => x["Email:Username"]).Returns((string?)null);
        mockConfig.Setup(x => x["Email:Password"]).Returns((string?)null);
        mockConfig.Setup(x => x["Email:FromEmail"]).Returns((string?)null);
        mockConfig.Setup(x => x["Email:FromName"]).Returns("Test App");

        // Act - Cria serviço com configurações vazias
        var service = new EmailService(mockConfig.Object, mockLogger.Object);

        // Assert - Serviço não deve ser nulo
        service.Should().NotBeNull();
        
        // Verifica se "NÃO CONFIGURADO" foi logado
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NÃO CONFIGURADO")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
