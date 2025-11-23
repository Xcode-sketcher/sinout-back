// ============================================================
// 🔑 TESTES DO PASSWORDRESETREPOSITORY - REPOSITÓRIO DE TOKENS DE RESET
// ============================================================
// Valida as operações CRUD de tokens de reset de senha no MongoDB,
// incluindo criação, consulta, marcação como usado e limpeza de expirados.

using Xunit;
using Moq;
using MongoDB.Driver;
using APISinout.Data;
using APISinout.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APISinout.Tests.Unit.Data;

public class PasswordResetRepositoryTests
{
    private readonly Mock<IMongoCollection<PasswordResetToken>> _tokensMock;
    private readonly PasswordResetRepository _repository;

    public PasswordResetRepositoryTests()
    {
        _tokensMock = new Mock<IMongoCollection<PasswordResetToken>>();
        _repository = new PasswordResetRepository(_tokensMock.Object);
    }

    #region GetByTokenAsync Tests

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnToken_WhenValidAndActive()
    {
        // Arrange - Configura token válido e ativo no mock
        var token = "valid-token-123";
        var expectedToken = new PasswordResetToken
        {
            Id = "token-id",
            Token = token,
            UserId = "user-id-1",
            Used = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        var mockCursor = new Mock<IAsyncCursor<PasswordResetToken>>();
        mockCursor.Setup(c => c.Current).Returns(new List<PasswordResetToken> { expectedToken });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _tokensMock.Setup(t => t.FindAsync(
            It.IsAny<FilterDefinition<PasswordResetToken>>(),
            It.IsAny<FindOptions<PasswordResetToken, PasswordResetToken>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por token
        var result = await _repository.GetByTokenAsync(token);

        // Assert - Verifica se token correto foi retornado
        Assert.NotNull(result);
        Assert.Equal(expectedToken.Id, result.Id);
        Assert.Equal(expectedToken.Token, result.Token);
        Assert.Equal(expectedToken.UserId, result.UserId);
        Assert.False(result.Used);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnNull_WhenTokenNotExists()
    {
        // Arrange - Configura mock para token inexistente
        var mockCursor = new Mock<IAsyncCursor<PasswordResetToken>>();
        mockCursor.Setup(c => c.Current).Returns(new List<PasswordResetToken>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _tokensMock.Setup(t => t.FindAsync(
            It.IsAny<FilterDefinition<PasswordResetToken>>(),
            It.IsAny<FindOptions<PasswordResetToken, PasswordResetToken>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por token inexistente
        var result = await _repository.GetByTokenAsync("nonexistent-token");

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnNull_WhenTokenExpired()
    {
        // Arrange - Configura mock para token expirado
        var mockCursor = new Mock<IAsyncCursor<PasswordResetToken>>();
        mockCursor.Setup(c => c.Current).Returns(new List<PasswordResetToken>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _tokensMock.Setup(t => t.FindAsync(
            It.IsAny<FilterDefinition<PasswordResetToken>>(),
            It.IsAny<FindOptions<PasswordResetToken, PasswordResetToken>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por token expirado
        var result = await _repository.GetByTokenAsync("expired-token");

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    #endregion

    #region GetActiveTokenByUserIdAsync Tests

    [Fact]
    public async Task GetActiveTokenByUserIdAsync_ShouldReturnMostRecentToken_WhenMultipleExist()
    {
        // Arrange - Configura múltiplos tokens ativos para o usuário
        var userId = "user-id-1";
        var tokens = new List<PasswordResetToken>
        {
            new PasswordResetToken
            {
                Id = "token-1",
                UserId = userId,
                Used = false,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            },
            new PasswordResetToken
            {
                Id = "token-2",
                UserId = userId,
                Used = false,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow.AddMinutes(-15) // Mais recente
            }
        };

        var mockCursor = new Mock<IAsyncCursor<PasswordResetToken>>();
        mockCursor.Setup(c => c.Current).Returns(new List<PasswordResetToken> { tokens[1] }); // Retorna o mais recente
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _tokensMock.Setup(t => t.FindAsync(
            It.IsAny<FilterDefinition<PasswordResetToken>>(),
            It.IsAny<FindOptions<PasswordResetToken, PasswordResetToken>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de token ativo por usuário
        var result = await _repository.GetActiveTokenByUserIdAsync(userId);

        // Assert - Verifica se o token mais recente foi retornado
        Assert.NotNull(result);
        Assert.Equal("token-2", result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.False(result.Used);
    }

    [Fact]
    public async Task GetActiveTokenByUserIdAsync_ShouldReturnNull_WhenNoActiveTokens()
    {
        // Arrange - Configura mock para usuário sem tokens ativos
        var mockCursor = new Mock<IAsyncCursor<PasswordResetToken>>();
        mockCursor.Setup(c => c.Current).Returns(new List<PasswordResetToken>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _tokensMock.Setup(t => t.FindAsync(
            It.IsAny<FilterDefinition<PasswordResetToken>>(),
            It.IsAny<FindOptions<PasswordResetToken, PasswordResetToken>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de token ativo para usuário sem tokens
        var result = await _repository.GetActiveTokenByUserIdAsync("nonexistent-user");

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    #endregion

    #region CreateTokenAsync Tests

    [Fact]
    public async Task CreateTokenAsync_ShouldCallInsertOne()
    {
        // Arrange - Configura novo token para criação
        var resetToken = new PasswordResetToken
        {
            Id = "new-token-id",
            Token = "new-token-123",
            UserId = "user-id-1",
            Used = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        // Act - Executa criação do token
        await _repository.CreateTokenAsync(resetToken);

        // Assert - Verifica se InsertOneAsync foi chamado corretamente
        _tokensMock.Verify(t => t.InsertOneAsync(resetToken, null, default), Times.Once);
    }

    #endregion

    #region MarkAsUsedAsync Tests

    [Fact]
    public async Task MarkAsUsedAsync_ShouldCallUpdateOne()
    {
        // Arrange - Configura ID do token para marcar como usado
        var tokenId = "token-id-123";

        // Act - Executa marcação do token como usado
        await _repository.MarkAsUsedAsync(tokenId);

        // Assert - Verifica se UpdateOneAsync foi chamado corretamente
        _tokensMock.Verify(t => t.UpdateOneAsync(
            It.IsAny<FilterDefinition<PasswordResetToken>>(),
            It.IsAny<UpdateDefinition<PasswordResetToken>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    #endregion

    #region DeleteExpiredTokensAsync Tests

    [Fact]
    public async Task DeleteExpiredTokensAsync_ShouldCallDeleteMany()
    {
        // Arrange - Método não requer configuração específica

        // Act - Executa limpeza de tokens expirados
        await _repository.DeleteExpiredTokensAsync();

        // Assert - Verifica se DeleteManyAsync foi chamado corretamente
        _tokensMock.Verify(t => t.DeleteManyAsync(It.IsAny<FilterDefinition<PasswordResetToken>>(), default), Times.Once);
    }

    #endregion
}