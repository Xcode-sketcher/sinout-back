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
using System.Threading.Tasks;

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
        var user = new User { UserId = 1, Name = "Test User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(1, "happy")).ReturnsAsync(0);
        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(1, "happy")).ReturnsAsync(new List<EmotionMapping>());
        _mappingRepoMock.Setup(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateMappingAsync(request, 1, "Cuidador");

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
            _service.CreateMappingAsync(request, 1, "Cuidador"));
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
            _service.CreateMappingAsync(request, 1, "Cuidador"));
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
            _service.CreateMappingAsync(request, 1, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_EmptyMessage_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "",
            Priority = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_MessageTooLong_ThrowsException()
    {
        // Arrange
        var longMessage = new string('a', 201); // 201 caracteres
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = longMessage,
            Priority = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidPriority_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 3 // Invalid - must be 1 or 2
        };

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_ExceedsLimitOf2_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };
        var user = new User { UserId = 1, Name = "Test User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(1, "happy")).ReturnsAsync(2); // Já tem 2

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_DuplicatePriority_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };
        var existingMapping = new EmotionMapping
        {
            Id = "existing-id",
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "moderate",
            MinPercentage = 50.0,
            Message = "Existing",
            Priority = 1
        };
        var user = new User { UserId = 1, Name = "Test User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(1, "happy")).ReturnsAsync(1);
        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(1, "happy"))
            .ReturnsAsync(new List<EmotionMapping> { existingMapping });

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_AccessDenied_ThrowsException()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 2, // Trying to create for another user
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };

        // Act & Assert - Cuidador trying to create for another user
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Cuidador"));
    }
    [Fact]
    public async Task CreateMappingAsync_AdminCanCreateForOtherUsers()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = 2, // Admin creating for another user
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Admin created",
            Priority = 1
        };
        var user = new User { UserId = 2, Name = "Other User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(2, "happy")).ReturnsAsync(0);
        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(2, "happy")).ReturnsAsync(new List<EmotionMapping>());
        _mappingRepoMock.Setup(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateMappingAsync(request, 1, "Admin");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(2);
        _mappingRepoMock.Verify(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>()), Times.Once);
    }
    [Fact]
    public async Task CreateMappingAsync_UserNotFound_ThrowsException()
    {
        // Arange
        var request = new EmotionMappingRequest
        {
            UserId = 999,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };
        _userRepoMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((User?)null);

    
        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, 1, "Admin"));

    }

    #endregion
}