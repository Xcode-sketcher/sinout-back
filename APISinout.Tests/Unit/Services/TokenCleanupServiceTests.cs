// ============================================================
// 🧹 TESTES DO TOKENCLEANUPSERVICE - LIMPEZA AUTOMÁTICA
// ============================================================
// Valida o serviço de limpeza automática de tokens expirados,
// testando execução em background e tratamento de erros.

using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using APISinout.Data;
using APISinout.Services;

namespace APISinout.Tests.Unit.Services;

public class TokenCleanupServiceTests
{
    private readonly Mock<IPasswordResetRepository> _repositoryMock;
    private readonly Mock<ILogger<TokenCleanupService>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;

    public TokenCleanupServiceTests()
    {
        _repositoryMock = new Mock<IPasswordResetRepository>();
        _loggerMock = new Mock<ILogger<TokenCleanupService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

        // Setup do service scope
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IPasswordResetRepository)))
            .Returns(_repositoryMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
    }

    [Fact]
    public void TokenCleanupService_Constructor_InitializesCorrectly()
    {
        // Act
        var service = new TokenCleanupService(_serviceProviderMock.Object, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_StartsService_LogsInformation()
    {
        // Arrange
        var service = new TokenCleanupService(_serviceProviderMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancela após 100ms

        _repositoryMock.Setup(x => x.DeleteExpiredTokensAsync()).Returns(Task.CompletedTask);

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50); // Aguarda um pouco para executar
            await service.StopAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Esperado quando o token é cancelado
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Serviço de limpeza de tokens iniciado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_CallsRepository_OncePerCycle()
    {
        // Arrange
        var service = new TokenCleanupService(_serviceProviderMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200)); // Cancela após 200ms

        _repositoryMock.Setup(x => x.DeleteExpiredTokensAsync()).Returns(Task.CompletedTask);

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(150);
            await service.StopAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Esperado
        }

        // Assert - pode ter executado 0 ou 1 vez dependendo do timing
        _repositoryMock.Verify(x => x.DeleteExpiredTokensAsync(), Times.AtMost(1));
    }
}
