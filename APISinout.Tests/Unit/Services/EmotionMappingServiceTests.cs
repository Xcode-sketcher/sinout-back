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

    #region GetMappingsByUserAsync Tests

    [Fact]
    public async Task GetMappingsByUserAsync_AccessDenied_ThrowsException()
    {
        // Arrange
        var userId = 1;
        var currentUserId = 2;
        var currentUserRole = "Cuidador";

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetMappingsByUserAsync(userId, currentUserId, currentUserRole));
    }

    [Fact]
    public async Task GetMappingsByUserAsync_UserNotFound_ThrowsException()
    {
        // Arrange
        var userId = 1;
        var currentUserId = 1;
        var currentUserRole = "Cuidador";

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetMappingsByUserAsync(userId, currentUserId, currentUserRole));
    }

    #endregion

    #region UpdateMappingAsync Tests

    [Fact]
    public async Task UpdateMappingAsync_MappingNotFound_ThrowsException()
    {
        // Arrange
        var id = "invalid-id";
        var request = new EmotionMappingRequest();
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((EmotionMapping?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateMappingAsync(id, request, 1, "Cuidador"));
    }

    [Fact]
    public async Task UpdateMappingAsync_AccessDenied_ThrowsException()
    {
        // Arrange
        var id = "mapping-id";
        var mapping = new EmotionMapping { Id = id, UserId = 1 };
        var request = new EmotionMappingRequest();
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateMappingAsync(id, request, 2, "Cuidador"));
    }

    [Fact]
    public async Task UpdateMappingAsync_ValidRequest_UpdatesMapping()
    {
        // Arrange
        var id = "mapping-id";
        var mapping = new EmotionMapping { Id = id, UserId = 1, Emotion = "sad" };
        var request = new EmotionMappingRequest
        {
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Updated Message",
            Priority = 1
        };
        var user = new User { UserId = 1, Name = "Test User" };

        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);
        _mappingRepoMock.Setup(x => x.UpdateMappingAsync(id, It.IsAny<EmotionMapping>())).Returns(Task.CompletedTask);
        _userRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _service.UpdateMappingAsync(id, request, 1, "Cuidador");

        // Assert
        result.Should().NotBeNull();
        result.Emotion.Should().Be("happy");
        result.Message.Should().Be("Updated Message");
        _mappingRepoMock.Verify(x => x.UpdateMappingAsync(id, It.IsAny<EmotionMapping>()), Times.Once);
    }

    #endregion

    #region DeleteMappingAsync Tests

    [Fact]
    public async Task DeleteMappingAsync_MappingNotFound_ThrowsException()
    {
        // Arrange
        var id = "invalid-id";
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((EmotionMapping?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.DeleteMappingAsync(id, 1, "Cuidador"));
    }

    [Fact]
    public async Task DeleteMappingAsync_AccessDenied_ThrowsException()
    {
        // Arrange
        var id = "mapping-id";
        var mapping = new EmotionMapping { Id = id, UserId = 1 };
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() =>
            _service.DeleteMappingAsync(id, 2, "Cuidador"));
    }

    [Fact]
    public async Task DeleteMappingAsync_ValidRequest_DeletesMapping()
    {
        // Arrange
        var id = "mapping-id";
        var mapping = new EmotionMapping { Id = id, UserId = 1 };
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);
        _mappingRepoMock.Setup(x => x.DeleteMappingAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteMappingAsync(id, 1, "Cuidador");

        // Assert
        _mappingRepoMock.Verify(x => x.DeleteMappingAsync(id), Times.Once);
    }

    #endregion

    #region FindMatchingRuleAsync Tests

    [Fact]
    public async Task FindMatchingRuleAsync_HighIntensity_Match()
    {
        // Arrange
        var userId = 1;
        var emotion = "happy";
        var percentage = 80.0;
        var mapping = new EmotionMapping 
        { 
            Id = "1", 
            IntensityLevel = "high", 
            MinPercentage = 70.0, 
            Message = "High Match",
            Priority = 1
        };

        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(userId, emotion))
            .ReturnsAsync(new List<EmotionMapping> { mapping });

        // Act
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert
        result.message.Should().Be("High Match");
        result.ruleId.Should().Be("1");
    }

    [Fact]
    public async Task FindMatchingRuleAsync_HighIntensity_NoMatch_LowPercentage()
    {
        // Arrange
        var userId = 1;
        var emotion = "happy";
        var percentage = 40.0; // Below 50% for high intensity
        var mapping = new EmotionMapping 
        { 
            Id = "1", 
            IntensityLevel = "high", 
            MinPercentage = 30.0, 
            Message = "High Match",
            Priority = 1
        };

        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(userId, emotion))
            .ReturnsAsync(new List<EmotionMapping> { mapping });

        // Act
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert
        result.message.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchingRuleAsync_ModerateIntensity_Match()
    {
        // Arrange
        var userId = 1;
        var emotion = "sad";
        var percentage = 40.0;
        var mapping = new EmotionMapping 
        { 
            Id = "2", 
            IntensityLevel = "moderate", 
            MinPercentage = 30.0, 
            Message = "Moderate Match",
            Priority = 1
        };

        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(userId, emotion))
            .ReturnsAsync(new List<EmotionMapping> { mapping });

        // Act
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert
        result.message.Should().Be("Moderate Match");
    }

    [Fact]
    public async Task FindMatchingRuleAsync_ModerateIntensity_NoMatch_HighPercentage()
    {
        // Arrange
        var userId = 1;
        var emotion = "sad";
        var percentage = 60.0; // Above 50% for moderate intensity
        var mapping = new EmotionMapping 
        { 
            Id = "2", 
            IntensityLevel = "moderate", 
            MinPercentage = 30.0, 
            Message = "Moderate Match",
            Priority = 1
        };

        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(userId, emotion))
            .ReturnsAsync(new List<EmotionMapping> { mapping });

        // Act
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert
        result.message.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchingMessageAsync_ReturnsMessage()
    {
        // Arrange
        var userId = 1;
        var emotion = "happy";
        var percentage = 80.0;
        var mapping = new EmotionMapping 
        { 
            Id = "1", 
            IntensityLevel = "high", 
            MinPercentage = 70.0, 
            Message = "High Match",
            Priority = 1
        };

        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(userId, emotion))
            .ReturnsAsync(new List<EmotionMapping> { mapping });

        // Act
        var result = await _service.FindMatchingMessageAsync(userId, emotion, percentage);

        // Assert
        result.Should().Be("High Match");
    }

    #endregion
}