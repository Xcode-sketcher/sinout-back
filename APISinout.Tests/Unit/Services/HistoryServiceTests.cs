// ============================================================
// 📊 TESTES DO HISTORYSERVICE - HISTÓRICO E ESTATÍSTICAS
// ============================================================
// Valida a lógica de negócio de histórico de análises,
// incluindo validações de permissões e geração de estatísticas.

using Xunit;
using FluentAssertions;
using Moq;
using APISinout.Data;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Tests.Unit.Services;

public class HistoryServiceTests
{
    private readonly Mock<IHistoryRepository> _historyRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly HistoryService _service;

    public HistoryServiceTests()
    {
        _historyRepoMock = new Mock<IHistoryRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _service = new HistoryService(_historyRepoMock.Object, _userRepoMock.Object);
    }

    #region GetHistoryByUserAsync Tests

    [Fact]
    public async Task GetHistoryByUserAsync_AsOwner_ReturnsHistory()
    {
        // Arrange
        var userId = 1;
        var records = new List<HistoryRecord>
        {
            new HistoryRecord
            {
                Id = "1",
                UserId = userId,
                DominantEmotion = "happy",
                DominantPercentage = 85.0,
                Timestamp = DateTime.UtcNow
            }
        };
        var user = new User { UserId = userId, Name = "Test User", Role = "Caregiver" };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _historyRepoMock.Setup(x => x.GetByUserIdAsync(userId, 24)).ReturnsAsync(records);

        // Act
        var result = await _service.GetHistoryByUserAsync(userId, userId, "Caregiver", 24);

        // Assert
        result.Should().HaveCount(1);
        result[0].DominantEmotion.Should().Be("happy");
        result[0].PatientName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetHistoryByUserAsync_AsAdmin_ReturnsHistory()
    {
        // Arrange
        var userId = 2;
        var adminId = 1;
        var records = new List<HistoryRecord>
        {
            new HistoryRecord
            {
                Id = "1",
                UserId = userId,
                DominantEmotion = "sad",
                DominantPercentage = 75.0,
                Timestamp = DateTime.UtcNow
            }
        };
        var user = new User { UserId = userId, Name = "Other User", Role = "Caregiver" };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _historyRepoMock.Setup(x => x.GetByUserIdAsync(userId, 24)).ReturnsAsync(records);

        // Act
        var result = await _service.GetHistoryByUserAsync(userId, adminId, "Admin", 24);

        // Assert
        result.Should().HaveCount(1);
        result[0].DominantEmotion.Should().Be("sad");
    }

    [Fact]
    public async Task GetHistoryByUserAsync_AccessDenied_ThrowsException()
    {
        // Arrange
        var userId = 2;
        var currentUserId = 1;

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetHistoryByUserAsync(userId, currentUserId, "Caregiver", 24));
    }
    #endregion
}
