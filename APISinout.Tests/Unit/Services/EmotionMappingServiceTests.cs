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
        // Arrange - Configura usuário válido e requisição de mapeamento
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Quero água",
            Priority = 1
        };
        var user = new User { Id = userId, Name = "Test User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(userId, "happy")).ReturnsAsync(0);
        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(userId, "happy")).ReturnsAsync(new List<EmotionMapping>());
        _mappingRepoMock.Setup(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>())).Returns(Task.CompletedTask);

        // Act - Chama CreateMappingAsync
        var result = await _service.CreateMappingAsync(request, userId, "Cuidador");

        // Assert - Verifica se o mapeamento foi criado com sucesso
        result.Should().NotBeNull();
        result.Emotion.Should().Be("happy");
        result.Message.Should().Be("Quero água");
        result.Priority.Should().Be(1);
        _mappingRepoMock.Verify(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>()), Times.Once);
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidEmotion_ThrowsException()
    {
        // Arrange - Configura requisição com emoção inválida
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "invalid",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };

        // Act & Assert - Deve lançar AppException (emoção inválida)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidIntensityLevel_ThrowsException()
    {
        // Arrange - Configura requisição com nível de intensidade inválido
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "invalid",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };

        // Act & Assert - Deve lançar AppException (nível de intensidade inválido)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidMinPercentage_ThrowsException()
    {
        // Arrange - Configura requisição com porcentagem mínima inválida
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 150.0, // Invalid - must be 0-100
            Message = "Test",
            Priority = 1
        };

        // Act & Assert - Deve lançar AppException (porcentagem mínima inválida)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_EmptyMessage_ThrowsException()
    {
        // Arrange - Configura requisição com mensagem vazia
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "",
            Priority = 1
        };

        // Act & Assert - Deve lançar AppException (mensagem vazia)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_MessageTooLong_ThrowsException()
    {
        // Arrange - Configura requisição com mensagem muito longa
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var longMessage = new string('a', 201); // 201 caracteres
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = longMessage,
            Priority = 1
        };

        // Act & Assert - Deve lançar AppException (mensagem muito longa)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidPriority_ThrowsException()
    {
        // Arrange - Configura requisição com prioridade inválida
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 3 // Invalid - must be 1 or 2
        };

        // Act & Assert - Deve lançar AppException (prioridade inválida)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_ExceedsLimitOf2_ThrowsException()
    {
        // Arrange - Configura requisição que excede limite de 2 mapeamentos por emoção
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };
        var user = new User { Id = userId, Name = "Test User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(userId, "happy")).ReturnsAsync(2); // Já tem 2

        // Act & Assert - Deve lançar AppException (excede limite de 2 mapeamentos por emoção)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_DuplicatePriority_ThrowsException()
    {
        // Arrange - Configura requisição com prioridade duplicada para mesma emoção
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };
        var existingMapping = new EmotionMapping
        {
            Id = "existing-id",
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "moderate",
            MinPercentage = 50.0,
            Message = "Existing",
            Priority = 1
        };
        var user = new User { Id = userId, Name = "Test User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(userId, "happy")).ReturnsAsync(1);
        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(userId, "happy"))
            .ReturnsAsync(new List<EmotionMapping> { existingMapping });

        // Act & Assert - Deve lançar AppException (prioridade duplicada para a mesma emoção)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }

    [Fact]
    public async Task CreateMappingAsync_AccessDenied_ThrowsException()
    {
        // Arrange - Configura tentativa de criação para outro usuário sem permissão
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var otherUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = otherUserId, // Trying to create for another user
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };

        // Act & Assert - Deve lançar AppException (cuidador tentando criar para outro usuário)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, userId, "Cuidador"));
    }
    [Fact]
    public async Task CreateMappingAsync_AdminCanCreateForOtherUsers()
    {
        // Arrange - Configura admin criando mapeamento para outro usuário
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var targetUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = targetUserId, // Admin creating for another user
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Admin created",
            Priority = 1
        };
        var user = new User { Id = targetUserId, Name = "Other User", Role = "Cuidador" };

        _userRepoMock.Setup(x => x.GetByIdAsync(targetUserId)).ReturnsAsync(user);
        _mappingRepoMock.Setup(x => x.CountByUserAndEmotionAsync(targetUserId, "happy")).ReturnsAsync(0);
        _mappingRepoMock.Setup(x => x.GetByUserAndEmotionAsync(targetUserId, "happy")).ReturnsAsync(new List<EmotionMapping>());
        _mappingRepoMock.Setup(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>())).Returns(Task.CompletedTask);

        // Act - Chama CreateMappingAsync como Admin
        var result = await _service.CreateMappingAsync(request, adminId, "Admin");

        // Assert - Verifica se o mapeamento foi criado com sucesso para o usuário alvo
        result.Should().NotBeNull();
        result.UserId.Should().Be(targetUserId);
        _mappingRepoMock.Verify(x => x.CreateMappingAsync(It.IsAny<EmotionMapping>()), Times.Once);
    }
    [Fact]
    public async Task CreateMappingAsync_UserNotFound_ThrowsException()
    {
        // Arrange - Configura requisição para usuário inexistente
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var invalidUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest
        {
            UserId = invalidUserId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Test",
            Priority = 1
        };
        _userRepoMock.Setup(x => x.GetByIdAsync(invalidUserId)).ReturnsAsync((User?)null);

    
        // Act & Assert - Deve lançar AppException (usuário inexistente)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateMappingAsync(request, adminId, "Admin"));

    }

    #endregion

    #region GetMappingsByUserAsync Tests

    [Fact]
    public async Task GetMappingsByUserAsync_AccessDenied_ThrowsException()
    {
        // Arrange - Configura tentativa de acesso negado a mapeamentos de outro usuário
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var currentUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var currentUserRole = "Cuidador";

        // Act & Assert - Deve lançar AppException (acesso negado)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetMappingsByUserAsync(userId, currentUserId, currentUserRole));
    }

    [Fact]
    public async Task GetMappingsByUserAsync_UserNotFound_ThrowsException()
    {
        // Arrange - Configura busca de mapeamentos para usuário inexistente
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var currentUserId = userId;
        var currentUserRole = "Cuidador";

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert - Deve lançar AppException (usuário não encontrado)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.GetMappingsByUserAsync(userId, currentUserId, currentUserRole));
    }

    #endregion

    #region UpdateMappingAsync Tests

    [Fact]
    public async Task UpdateMappingAsync_MappingNotFound_ThrowsException()
    {
        // Arrange - Configura tentativa de atualização de mapeamento inexistente
        var id = "invalid-id";
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = new EmotionMappingRequest();
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((EmotionMapping?)null);

        // Act & Assert - Deve lançar AppException (mapeamento inexistente)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateMappingAsync(id, request, userId, "Cuidador"));
    }

    [Fact]
    public async Task UpdateMappingAsync_AccessDenied_ThrowsException()
    {
        // Arrange - Configura tentativa de atualização sem permissão de acesso
        var id = "mapping-id";
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var otherUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var mapping = new EmotionMapping { Id = id, UserId = userId };
        var request = new EmotionMappingRequest();
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);

        // Act & Assert - Deve lançar AppException (acesso negado)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateMappingAsync(id, request, otherUserId, "Cuidador"));
    }

    [Fact]
    public async Task UpdateMappingAsync_ValidRequest_UpdatesMapping()
    {
        // Arrange - Configura atualização válida de mapeamento existente
        var id = "mapping-id";
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var mapping = new EmotionMapping { Id = id, UserId = userId, Emotion = "sad" };
        var request = new EmotionMappingRequest
        {
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Updated Message",
            Priority = 1
        };
        var user = new User { Id = userId, Name = "Test User" };

        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);
        _mappingRepoMock.Setup(x => x.UpdateMappingAsync(id, It.IsAny<EmotionMapping>())).Returns(Task.CompletedTask);
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act - Chama FindMatchingRuleAsync
        var result = await _service.UpdateMappingAsync(id, request, userId, "Cuidador");

        // Assert - Verifica se a mensagem e ruleId correspondem ao mapeamento
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
        // Arrange - Configura tentativa de exclusão de mapeamento inexistente
        var id = "invalid-id";
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((EmotionMapping?)null);

        // Act & Assert - Deve lançar AppException (mapeamento inexistente)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.DeleteMappingAsync(id, userId, "Cuidador"));
    }

    [Fact]
    public async Task DeleteMappingAsync_AccessDenied_ThrowsException()
    {
        // Arrange - Configura tentativa de exclusão sem permissão de acesso
        var id = "mapping-id";
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var otherUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var mapping = new EmotionMapping { Id = id, UserId = userId };
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);

        // Act & Assert - Deve lançar AppException (acesso negado)
        await Assert.ThrowsAsync<AppException>(() =>
            _service.DeleteMappingAsync(id, otherUserId, "Cuidador"));
    }

    [Fact]
    public async Task DeleteMappingAsync_ValidRequest_DeletesMapping()
    {
        // Arrange - Configura exclusão válida de mapeamento existente
        var id = "mapping-id";
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var mapping = new EmotionMapping { Id = id, UserId = userId };
        
        _mappingRepoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(mapping);
        _mappingRepoMock.Setup(x => x.DeleteMappingAsync(id)).Returns(Task.CompletedTask);

        // Act - Chama DeleteMappingAsync
        await _service.DeleteMappingAsync(id, userId, "Cuidador");

        // Assert - Verifica se DeleteMappingAsync foi chamado
        _mappingRepoMock.Verify(x => x.DeleteMappingAsync(id), Times.Once);
    }

    #endregion

    #region FindMatchingRuleAsync Tests

    [Fact]
    public async Task FindMatchingRuleAsync_HighIntensity_Match()
    {
        // Arrange - Configura busca de regra com intensidade alta que deve corresponder
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
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

        // Act - Chama FindMatchingRuleAsync
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert - Verifica se não foi encontrada mensagem (sem correspondência)
        result.message.Should().Be("High Match");
        result.ruleId.Should().Be("1");
    }

    [Fact]
    public async Task FindMatchingRuleAsync_HighIntensity_NoMatch_LowPercentage()
    {
        // Arrange - Configura busca de regra com intensidade alta que não deve corresponder por baixa porcentagem
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
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

        // Act - Chama FindMatchingRuleAsync
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert - Verifica se a mensagem corresponde ao mapeamento (Moderate Match)
        result.message.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchingRuleAsync_ModerateIntensity_Match()
    {
        // Arrange - Configura busca de regra com intensidade moderada que deve corresponder
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
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

        // Act - Chama FindMatchingRuleAsync
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert - Verifica se não foi encontrada mensagem (sem correspondência para porcentagem)
        result.message.Should().Be("Moderate Match");
    }

    [Fact]
    public async Task FindMatchingRuleAsync_ModerateIntensity_NoMatch_HighPercentage()
    {
        // Arrange - Configura busca de regra com intensidade moderada que não deve corresponder por alta porcentagem
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
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

        // Act - Chama FindMatchingMessageAsync
        var result = await _service.FindMatchingRuleAsync(userId, emotion, percentage);

        // Assert - Verifica se a mensagem retornada corresponde a "High Match"
        result.message.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchingMessageAsync_ReturnsMessage()
    {
        // Arrange - Configura busca de mensagem correspondente para emoção e porcentagem
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
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

        // Act - Chama FindMatchingMessageAsync
        var result = await _service.FindMatchingMessageAsync(userId, emotion, percentage);

        // Assert - Verifica se a mensagem retornada corresponde a "High Match"
        result.Should().Be("High Match");
    }

    #endregion
}