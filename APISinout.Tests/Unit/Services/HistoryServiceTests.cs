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
using System.Threading.Tasks;

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
        var user = new User { UserId = userId, Name = "Test User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _historyRepoMock.Setup(x => x.GetByUserIdAsync(userId, 24)).ReturnsAsync(records);

        // Act
        var result = await _service.GetHistoryByUserAsync(userId, userId, "Cuidador", 24);

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
        var user = new User { UserId = userId, Name = "Other User", Role = "Cuidador" };

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
            _service.GetHistoryByUserAsync(userId, currentUserId, "Cuidador", 24));
    }
    [Fact]
    public async Task GetHistoryByUserAsync_UserNotFound_ThrowsException()
    {
        // Given
        var userId = 999;
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetHistoryByUserAsync(userId, 1, "Admin", 24));
    }
    #endregion

    #region GetHistoryByFilterAsync Tests

    [Fact]
    public async Task GetHistoryByFilterAsync_AsAdmin_ReturnsFilteredRecords()
    {
        // Arrange
        var filter = new HistoryFilter { PatientId = 1 };
        var records = new List<HistoryRecord>
        {
            new HistoryRecord { Id = "1", UserId = 1, DominantEmotion = "happy" }
        };
        var user = new User { UserId = 1, Name = "Test User" };

        _historyRepoMock.Setup(x => x.GetByFilterAsync(filter)).ReturnsAsync(records);
        _userRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _service.GetHistoryByFilterAsync(filter, 2, "Admin");

        // Assert
        result.Should().HaveCount(1);
        result[0].PatientName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetHistoryByFilterAsync_AsCuidador_RestrictsToOwnHistory()
    {
        // Arrange
        var filter = new HistoryFilter { PatientId = 999 }; // Tenta buscar outro
        var userId = 1;
        var records = new List<HistoryRecord>();

        _historyRepoMock.Setup(x => x.GetByFilterAsync(It.Is<HistoryFilter>(f => f.PatientId == userId)))
            .ReturnsAsync(records);

        // Act
        await _service.GetHistoryByFilterAsync(filter, userId, "Cuidador");

        // Assert
        _historyRepoMock.Verify(x => x.GetByFilterAsync(It.Is<HistoryFilter>(f => f.PatientId == userId)), Times.Once);
    }

    #endregion

    #region GetUserStatisticsAsync Tests

    [Fact]
    public async Task GetUserStatisticsAsync_AsOwner_ReturnsStatistics()
    {
        // Arrange
        var userId = 1;
        var stats = new PatientStatistics { TotalAnalyses = 10, MostFrequentEmotion = "happy" };
        var user = new User { UserId = userId, Name = "Test User" };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _historyRepoMock.Setup(x => x.GetUserStatisticsAsync(userId, 24)).ReturnsAsync(stats);

        // Act
        var result = await _service.GetUserStatisticsAsync(userId, userId, "Cuidador", 24);

        // Assert
        result.Should().NotBeNull();
        result.PatientName.Should().Be("Test User");
        result.TotalAnalyses.Should().Be(10);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_AccessDenied_ThrowsException()
    {
        // Arrange
        var userId = 2;
        var currentUserId = 1;

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetUserStatisticsAsync(userId, currentUserId, "Cuidador", 24));
    }

    #endregion

    #region Other Methods Tests

    [Fact]
    public async Task CleanOldHistoryAsync_ShouldCallRepositoryDelete()
    {
        // Arrange
        var hours = 48;

        // Act
        await _service.CleanOldHistoryAsync(hours);

        // Assert
        _historyRepoMock.Verify(x => x.DeleteOldRecordsAsync(hours), Times.Once);
    }

    [Fact]
    public async Task CreateHistoryRecordAsync_ShouldCallRepositoryCreate()
    {
        // Arrange
        var record = new HistoryRecord { UserId = 1, DominantEmotion = "happy" };

        // Act
        await _service.CreateHistoryRecordAsync(record);

        // Assert
        _historyRepoMock.Verify(x => x.CreateRecordAsync(record), Times.Once);
    }

    #endregion
}
