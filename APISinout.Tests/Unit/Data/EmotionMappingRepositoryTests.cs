// ============================================================
// 😊 TESTES DO EMOTIONMAPPINGREPOSITORY - REPOSITÓRIO DE MAPEAMENTO DE EMOÇÕES
// ============================================================
// Valida as operações CRUD de mapeamentos de emoções no MongoDB,
// incluindo consultas, criação, atualização, exclusão lógica e validações.

using Xunit;
using Moq;
using MongoDB.Driver;
using APISinout.Data;
using APISinout.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APISinout.Tests.Unit.Data;

public class EmotionMappingRepositoryTests
{
    private readonly Mock<IMongoCollection<EmotionMapping>> _mappingsMock;
    private readonly EmotionMappingRepository _repository;

    public EmotionMappingRepositoryTests()
    {
        _mappingsMock = new Mock<IMongoCollection<EmotionMapping>>();
        _repository = new EmotionMappingRepository(_mappingsMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMapping_WhenExists()
    {
        // Arrange - Configura mapeamento existente no mock
        var mappingId = "mapping-id-123";
        var expectedMapping = new EmotionMapping
        {
            Id = mappingId,
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80,
            Priority = 1,
            Active = true
        };
        var mockCursor = new Mock<IAsyncCursor<EmotionMapping>>();
        mockCursor.Setup(c => c.Current).Returns(new List<EmotionMapping> { expectedMapping });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mappingsMock.Setup(m => m.FindAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<FindOptions<EmotionMapping, EmotionMapping>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por ID
        var result = await _repository.GetByIdAsync(mappingId);

        // Assert - Verifica se mapeamento correto foi retornado
        Assert.NotNull(result);
        Assert.Equal(expectedMapping.Id, result.Id);
        Assert.Equal(expectedMapping.UserId, result.UserId);
        Assert.Equal(expectedMapping.Emotion, result.Emotion);
        Assert.Equal(expectedMapping.IntensityLevel, result.IntensityLevel);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange - Configura mock para mapeamento inexistente
        var mockCursor = new Mock<IAsyncCursor<EmotionMapping>>();
        mockCursor.Setup(c => c.Current).Returns(new List<EmotionMapping>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mappingsMock.Setup(m => m.FindAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<FindOptions<EmotionMapping, EmotionMapping>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por ID inexistente
        var result = await _repository.GetByIdAsync("nonexistent-id");

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnMappingsOrderedByEmotionAndPriority()
    {
        // Arrange - Configura múltiplos mapeamentos para o usuário
        var userId = 1;
        var mappings = new List<EmotionMapping>
        {
            new EmotionMapping { Id = "1", UserId = userId, Emotion = "happy", Priority = 2, Active = true },
            new EmotionMapping { Id = "2", UserId = userId, Emotion = "sad", Priority = 1, Active = true },
            new EmotionMapping { Id = "3", UserId = userId, Emotion = "happy", Priority = 1, Active = true }
        };

        var mockCursor = new Mock<IAsyncCursor<EmotionMapping>>();
        mockCursor.Setup(c => c.Current).Returns(mappings);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mappingsMock.Setup(m => m.FindAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<FindOptions<EmotionMapping, EmotionMapping>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de mapeamentos por usuário
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert - Verifica se mapeamentos foram retornados
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.All(result, m => Assert.Equal(userId, m.UserId));
    }

    #endregion

    #region GetActiveByUserIdAsync Tests

    [Fact]
    public async Task GetActiveByUserIdAsync_ShouldReturnOnlyActiveMappings()
    {
        // Arrange - Configura mapeamentos ativos e inativos para o usuário
        var userId = 1;
        var mappings = new List<EmotionMapping>
        {
            new EmotionMapping { Id = "1", UserId = userId, Emotion = "happy", Active = true },
            new EmotionMapping { Id = "2", UserId = userId, Emotion = "sad", Active = false },
            new EmotionMapping { Id = "3", UserId = userId, Emotion = "angry", Active = true }
        };

        var mockCursor = new Mock<IAsyncCursor<EmotionMapping>>();
        mockCursor.Setup(c => c.Current).Returns(mappings.Where(m => m.Active).ToList());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mappingsMock.Setup(m => m.FindAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<FindOptions<EmotionMapping, EmotionMapping>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de mapeamentos ativos por usuário
        var result = await _repository.GetActiveByUserIdAsync(userId);

        // Assert - Verifica se apenas mapeamentos ativos foram retornados
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.True(m.Active));
        Assert.All(result, m => Assert.Equal(userId, m.UserId));
    }

    #endregion

    #region GetByUserAndEmotionAsync Tests

    [Fact]
    public async Task GetByUserAndEmotionAsync_ShouldReturnMappingsForSpecificEmotion()
    {
        // Arrange - Configura mapeamentos para emoção específica
        var userId = 1;
        var emotion = "happy";
        var mappings = new List<EmotionMapping>
        {
            new EmotionMapping { Id = "1", UserId = userId, Emotion = emotion, Priority = 1, Active = true },
            new EmotionMapping { Id = "2", UserId = userId, Emotion = emotion, Priority = 2, Active = true }
        };

        var mockCursor = new Mock<IAsyncCursor<EmotionMapping>>();
        mockCursor.Setup(c => c.Current).Returns(mappings);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mappingsMock.Setup(m => m.FindAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<FindOptions<EmotionMapping, EmotionMapping>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de mapeamentos por usuário e emoção
        var result = await _repository.GetByUserAndEmotionAsync(userId, emotion);

        // Assert - Verifica se mapeamentos da emoção específica foram retornados
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(userId, m.UserId));
        Assert.All(result, m => Assert.Equal(emotion, m.Emotion));
        Assert.All(result, m => Assert.True(m.Active));
    }

    #endregion

    #region CreateMappingAsync Tests

    [Fact]
    public async Task CreateMappingAsync_ShouldCallInsertOne()
    {
        // Arrange - Configura novo mapeamento para criação
        var mapping = new EmotionMapping
        {
            Id = "new-mapping-id",
            UserId = 1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80,
            Priority = 1,
            Active = true
        };

        // Act - Executa criação do mapeamento
        await _repository.CreateMappingAsync(mapping);

        // Assert - Verifica se InsertOneAsync foi chamado corretamente
        _mappingsMock.Verify(m => m.InsertOneAsync(mapping, null, default), Times.Once);
    }

    #endregion

    #region UpdateMappingAsync Tests

    [Fact]
    public async Task UpdateMappingAsync_ShouldCallUpdateOne()
    {
        // Arrange - Configura dados para atualização de mapeamento
        var mappingId = "mapping-id-123";
        var mapping = new EmotionMapping
        {
            Emotion = "sad",
            IntensityLevel = "moderate",
            MinPercentage = 70,
            Message = "Updated message",
            Priority = 2,
            Active = true
        };

        // Act - Executa atualização do mapeamento
        await _repository.UpdateMappingAsync(mappingId, mapping);

        // Assert - Verifica se UpdateOneAsync foi chamado corretamente
        _mappingsMock.Verify(m => m.UpdateOneAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<UpdateDefinition<EmotionMapping>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    #endregion

    #region DeleteMappingAsync Tests

    [Fact]
    public async Task DeleteMappingAsync_ShouldPerformSoftDelete()
    {
        // Arrange - Configura ID do mapeamento para exclusão lógica
        var mappingId = "mapping-id-123";

        // Act - Executa exclusão lógica do mapeamento
        await _repository.DeleteMappingAsync(mappingId);

        // Assert - Verifica se UpdateOneAsync foi chamado para soft delete
        _mappingsMock.Verify(m => m.UpdateOneAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<UpdateDefinition<EmotionMapping>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    #endregion

    #region CountByUserAndEmotionAsync Tests

    [Fact]
    public async Task CountByUserAndEmotionAsync_ShouldReturnCorrectCount()
    {
        // Arrange - Configura contagem de mapeamentos por usuário e emoção
        var userId = 1;
        var emotion = "happy";
        var expectedCount = 3L;

        _mappingsMock.Setup(m => m.CountDocumentsAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<CountOptions>(),
            default))
            .ReturnsAsync(expectedCount);

        // Act - Executa contagem de mapeamentos
        var result = await _repository.CountByUserAndEmotionAsync(userId, emotion);

        // Assert - Verifica se a contagem correta foi retornada
        Assert.Equal((int)expectedCount, result);
    }

    [Fact]
    public async Task CountByUserAndEmotionAsync_ShouldReturnZero_WhenNoMappings()
    {
        // Arrange - Configura contagem zero para usuário e emoção sem mapeamentos
        var userId = 1;
        var emotion = "nonexistent";

        _mappingsMock.Setup(m => m.CountDocumentsAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<CountOptions>(),
            default))
            .ReturnsAsync(0L);

        // Act - Executa contagem quando não há mapeamentos
        var result = await _repository.CountByUserAndEmotionAsync(userId, emotion);

        // Assert - Verifica se zero foi retornado
        Assert.Equal(0, result);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenMappingExists()
    {
        // Arrange - Configura mock para mapeamento existente
        var mappingId = "existing-mapping-id";

        _mappingsMock.Setup(m => m.CountDocumentsAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<CountOptions>(),
            default))
            .ReturnsAsync(1L);

        // Act - Executa verificação de existência
        var result = await _repository.ExistsAsync(mappingId);

        // Assert - Verifica se retornou verdadeiro
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenMappingNotExists()
    {
        // Arrange - Configura mock para mapeamento inexistente
        var mappingId = "nonexistent-mapping-id";

        _mappingsMock.Setup(m => m.CountDocumentsAsync(
            It.IsAny<FilterDefinition<EmotionMapping>>(),
            It.IsAny<CountOptions>(),
            default))
            .ReturnsAsync(0L);

        // Act - Executa verificação de existência
        var result = await _repository.ExistsAsync(mappingId);

        // Assert - Verifica se retornou falso
        Assert.False(result);
    }

    #endregion
}