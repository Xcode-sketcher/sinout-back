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
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly HistoryService _service;

    public HistoryServiceTests()
    {
        _historyRepoMock = new Mock<IHistoryRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _patientRepoMock = new Mock<IPatientRepository>();
        _service = new HistoryService(_historyRepoMock.Object, _userRepoMock.Object, _patientRepoMock.Object);
    }

    #region GetHistoryByPatientAsync Tests

    [Fact]
    public async Task GetHistoryByPatientAsync_AsOwner_ReturnsHistory()
    {
        // Arrange - Prepara dados e mocks para o teste
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var records = new List<HistoryRecord>
        {
            new HistoryRecord
            {
                Id = "1",
                UserId = userId,
                PatientId = patientId,
                DominantEmotion = "happy",
                DominantPercentage = 85.0,
                Timestamp = DateTime.UtcNow
            }
        };
        var patient = new Patient { Id = patientId, Name = "Test Patient", CuidadorId = userId };

        _patientRepoMock.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(patient);
        _historyRepoMock.Setup(x => x.GetByPatientIdAsync(patientId, 24)).ReturnsAsync(records);

        // Act - Executa a ação sob teste
        var result = await _service.GetHistoryByPatientAsync(patientId, userId, "Cuidador", 24);

        // Assert - Verifica o resultado esperado
        result.Should().HaveCount(1);
        result[0].DominantEmotion.Should().Be("happy");
        result[0].PatientName.Should().Be("Test Patient");
    }

    [Fact]
    public async Task GetHistoryByPatientAsync_AsAdmin_ReturnsHistory()
    {
        // Arrange - Prepara dados e mocks para o teste
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var records = new List<HistoryRecord>
        {
            new HistoryRecord
            {
                Id = "1",
                UserId = userId,
                PatientId = patientId,
                DominantEmotion = "sad",
                DominantPercentage = 75.0,
                Timestamp = DateTime.UtcNow
            }
        };
        var patient = new Patient { Id = patientId, Name = "Test Patient", CuidadorId = userId };

        _patientRepoMock.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(patient);
        _historyRepoMock.Setup(x => x.GetByPatientIdAsync(patientId, 24)).ReturnsAsync(records);

        // Act - Executa a ação sob teste
        var result = await _service.GetHistoryByPatientAsync(patientId, adminId, "Admin", 24);

        // Assert - Verifica o resultado esperado
        result.Should().HaveCount(1);
        result[0].DominantEmotion.Should().Be("sad");
    }

    [Fact]
    public async Task GetHistoryByPatientAsync_AccessDenied_ThrowsException()
    {
        // Arrange - Preparar dados e mocks para o teste
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var currentUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(); // Outro usuário
        var patient = new Patient { Id = patientId, Name = "Test Patient", CuidadorId = userId };

        _patientRepoMock.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(patient);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetHistoryByPatientAsync(patientId, currentUserId, "Cuidador", 24));
    }

    [Fact]
    public async Task GetHistoryByPatientAsync_PatientNotFound_ThrowsException()
    {
        // Arrange - Configura paciente não encontrado para reproduzir a condição
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        _patientRepoMock.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync((Patient?)null);

        // Act & Assert - Executa ação e verifica se lança exceção
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetHistoryByPatientAsync(patientId, adminId, "Admin", 24));
    }
    #endregion

    #region GetHistoryByFilterAsync Tests

    [Fact]
    public async Task GetHistoryByFilterAsync_AsAdmin_ReturnsFilteredRecords()
    {
        // Arrange - Prepara dados e mocks para o teste
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var filter = new HistoryFilter { PatientId = patientId };
        var records = new List<HistoryRecord>
        {
            new HistoryRecord { Id = "1", UserId = userId, PatientId = patientId, DominantEmotion = "happy" }
        };
        
        _historyRepoMock.Setup(x => x.GetByFilterAsync(filter)).ReturnsAsync(records);

        // Act - Executa a ação sob teste
        var result = await _service.GetHistoryByFilterAsync(filter, adminId, "Admin");

        // Assert - Verifica o resultado esperado
        result.Should().HaveCount(1);
        result[0].PatientId.Should().Be(patientId);
    }

    [Fact]
    public async Task GetHistoryByFilterAsync_AsCuidador_RestrictsToOwnHistory()
    {
        // Arrange - Prepara dados e mocks para o teste
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var filter = new HistoryFilter { PatientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString() }; // Tenta buscar outro
        var records = new List<HistoryRecord>();

        _historyRepoMock.Setup(x => x.GetByFilterAsync(It.Is<HistoryFilter>(f => f.CuidadorId == userId)))
            .ReturnsAsync(records);

        // Act - Executa a ação sob teste
        await _service.GetHistoryByFilterAsync(filter, userId, "Cuidador");

        // Assert - Verifica o resultado esperado
        _historyRepoMock.Verify(x => x.GetByFilterAsync(It.Is<HistoryFilter>(f => f.CuidadorId == userId)), Times.Once);
    }

    #endregion

    #region GetPatientStatisticsAsync Tests

    [Fact]
    public async Task GetPatientStatisticsAsync_AsOwner_ReturnsStatistics()
    {
        // Arrange - Prepara dados e mocks para o teste
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var stats = new PatientStatistics { TotalAnalyses = 10, MostFrequentEmotion = "happy" };
        var patient = new Patient { Id = patientId, Name = "Test Patient", CuidadorId = userId };

        _patientRepoMock.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(patient);
        _historyRepoMock.Setup(x => x.GetPatientStatisticsAsync(patientId, 24)).ReturnsAsync(stats);

        // Act - Executa a ação sob teste
        var result = await _service.GetPatientStatisticsAsync(patientId, userId, "Cuidador", 24);

        // Assert - Verifica o resultado esperado
        result.Should().NotBeNull();
        result.PatientName.Should().Be("Test Patient");
        result.TotalAnalyses.Should().Be(10);
    }

    [Fact]
    public async Task GetPatientStatisticsAsync_AccessDenied_ThrowsException()
    {
        // Arrange - Prepara dados e mocks para o teste
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var currentUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(); // Outro usuário
        var patient = new Patient { Id = patientId, Name = "Test Patient", CuidadorId = userId };

        _patientRepoMock.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(patient);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetPatientStatisticsAsync(patientId, currentUserId, "Cuidador", 24));
    }

    #endregion

    #region Other Methods Tests

    [Fact]
    public async Task CleanOldHistoryAsync_ShouldCallRepositoryDelete()
    {
        // Arrange - Preparar dados e mocks para o teste
        var hours = 48;

        // Act - Executa a ação sob teste
        await _service.CleanOldHistoryAsync(hours);

        // Assert - Verifica o resultado esperado
        _historyRepoMock.Verify(x => x.DeleteOldRecordsAsync(hours), Times.Once);
    }

    [Fact]
    public async Task CreateHistoryRecordAsync_ShouldCallRepositoryCreate()
    {
        // Arrange - Preparar dados e mocks para o teste
        var record = new HistoryRecord { UserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), DominantEmotion = "happy" };

        // Act - Executa a ação sob teste
        await _service.CreateHistoryRecordAsync(record);

        // Assert - Verifica o resultado esperado
        _historyRepoMock.Verify(x => x.CreateRecordAsync(record), Times.Once);
    }

    #endregion
}
