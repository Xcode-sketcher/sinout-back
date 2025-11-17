// ============================================================
// 😊 TESTES DO EMOTIONMAPPINGSERVICE - REGRAS DE EMOÇÕES
// ============================================================
// Valida a lógica de negócio de mapeamento de emoções,
// incluindo validações, limites de 2 por emoção, e busca de mensagens.

using Xunit;
using FluentAssertions;
using Moq;
using APISinout.Data;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Tests.Unit.Services;

public class EmotionMappingServiceTests
{
    private readonly Mock<IEmotionMappingRepository> _mappingRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly EmotionMappingService _service;

    public EmotionMappingServiceTests()
    {
        _mappingRepoMock = new Mock<IEmotionMappingRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _service = new EmotionMappingService(_mappingRepoMock.Object, _userRepoMock.Object);
    }

    #region CreateMappingAsync Tests

    [Fact]
    public async Task CreateMappingAsync_ValidRequest_CreatesMapping()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Quero água",
            Priority = 1
        };
        var user = new User { UserId = 1, Name = "Test User", Role = "Caregiver" };

        _userRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(1, "happy")).ReturnsAsync(0);
        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(1, "happy")).ReturnsAsync(new List<EmotionMapping>());
        _mappingRepoMock.Setup(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateMappingAsync(request, 1, "Caregiver");

        // Assert
        result.Should().NotBeNull();
        result.Emotion.Should().Be("happy");
        result.Message.Should().Be("Quero água");
        result.Priority.Should().Be(1);
        _mappingRepoMock.Verify(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>()), Times.Once);
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidEmotion_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "invalid",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Caregiver"));
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidIntensityLevel_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "invalid",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Caregiver"));
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidMinPercentage_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 150.0, // Invalid - must be 0-100
            Message = "Test",
            Priority = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Caregiver"));
    }
    #endregion
}
