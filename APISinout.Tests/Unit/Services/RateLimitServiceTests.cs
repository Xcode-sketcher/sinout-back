// ============================================================
// 🛡️ TESTES DO RATELIMITSERVICE - PROTEÇÃO CONTRA SPAM
// ============================================================
// Valida que o serviço de rate limiting funciona corretamente
// para proteger endpoints sensíveis contra abuso e spam.

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using APISinout.Services;

namespace APISinout.Tests.Unit.Services;

public class RateLimitServiceTests
{
    private readonly Mock<ILogger<RateLimitService>> _loggerMock;
    private readonly RateLimitService _service;

    public RateLimitServiceTests()
    {
        _loggerMock = new Mock<ILogger<RateLimitService>>();
        _service = new RateLimitService(_loggerMock.Object);
    }

    [Fact]
    public void IsRateLimited_NoAttempts_ReturnsFalse()
    {
        // Arrange
        var key = "test@example.com";

        // Act
        var result = _service.IsRateLimited(key, maxAttempts: 3, windowMinutes: 15);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_BelowLimit_ReturnsFalse()
    {
        // Arrange
        var key = "test@example.com";
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);

        // Act
        var result = _service.IsRateLimited(key, maxAttempts: 3, windowMinutes: 15);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_AtLimit_ReturnsTrue()
    {
        // Arrange
        var key = "test@example.com";
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);

        // Act
        var result = _service.IsRateLimited(key, maxAttempts: 3, windowMinutes: 15);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRateLimited_AboveLimit_ReturnsTrue()
    {
        // Arrange
        var key = "test@example.com";
        for (int i = 0; i < 5; i++)
        {
            _service.RecordAttempt(key);
        }

        // Act
        var result = _service.IsRateLimited(key, maxAttempts: 3, windowMinutes: 15);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RecordAttempt_AddsAttemptToKey()
    {
        // Arrange
        var key = "test@example.com";

        // Act
        _service.RecordAttempt(key);

        // Assert
        var isLimited = _service.IsRateLimited(key, maxAttempts: 1, windowMinutes: 15);
        isLimited.Should().BeTrue();
    }

    [Fact]
    public void RecordAttempt_MultipleKeys_TracksIndependently()
    {
        // Arrange
        var key1 = "user1@example.com";
        var key2 = "user2@example.com";

        // Act
        _service.RecordAttempt(key1);
        _service.RecordAttempt(key1);
        _service.RecordAttempt(key1);
        _service.RecordAttempt(key2);

        // Assert
        _service.IsRateLimited(key1, maxAttempts: 3, windowMinutes: 15).Should().BeTrue();
        _service.IsRateLimited(key2, maxAttempts: 3, windowMinutes: 15).Should().BeFalse();
    }

    [Fact]
    public void ClearAttempts_RemovesAttemptsForKey()
    {
        // Arrange
        var key = "test@example.com";
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);

        // Act
        _service.ClearAttempts(key);

        // Assert
        var isLimited = _service.IsRateLimited(key, maxAttempts: 3, windowMinutes: 15);
        isLimited.Should().BeFalse();
    }

    [Fact]
    public void ClearAttempts_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var key = "nonexistent@example.com";

        // Act & Assert
        var action = () => _service.ClearAttempts(key);
        action.Should().NotThrow();
    }

    [Fact]
    public void IsRateLimited_DifferentLimits_AppliesCorrectly()
    {
        // Arrange
        var key = "test@example.com";
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);

        // Act & Assert - com limite 2
        _service.IsRateLimited(key, maxAttempts: 2, windowMinutes: 15).Should().BeTrue();
        
        // Act & Assert - com limite 3
        _service.IsRateLimited(key, maxAttempts: 3, windowMinutes: 15).Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_LogsWarning_WhenLimitExceeded()
    {
        // Arrange
        var key = "test@example.com";
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);
        _service.RecordAttempt(key);

        // Act
        _service.IsRateLimited(key, maxAttempts: 3, windowMinutes: 15);

        // Assert - verifica que o log de warning foi chamado
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bloqueado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordAttempt_LogsInformation()
    {
        // Arrange
        var key = "test@example.com";

        // Act
        _service.RecordAttempt(key);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tentativa registrada")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ClearAttempts_LogsInformation()
    {
        // Arrange
        var key = "test@example.com";
        _service.RecordAttempt(key);

        // Act
        _service.ClearAttempts(key);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tentativas limpas")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void IsRateLimited_ConcurrentAccess_HandlesCorrectly()
    {
        // Arrange
        var key = "test@example.com";
        var tasks = new List<Task>();

        // Act - simula acesso concorrente
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _service.RecordAttempt(key)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        var isLimited = _service.IsRateLimited(key, maxAttempts: 5, windowMinutes: 15);
        isLimited.Should().BeTrue();
    }
}
