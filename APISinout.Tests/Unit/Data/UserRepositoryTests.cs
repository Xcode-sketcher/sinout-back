// ============================================================
// 👥 TESTES DO USERREPOSITORY - REPOSITÓRIO DE USUÁRIOS
// ============================================================
// Valida as operações CRUD de usuários no MongoDB,
// incluindo consultas, criação, atualização, exclusão e geração de IDs.

using Xunit;
using Moq;
using MongoDB.Driver;
using APISinout.Data;
using APISinout.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace APISinout.Tests.Unit.Data;

public class UserRepositoryTests
{
    private readonly Mock<IMongoCollection<User>> _usersMock;
    private readonly Mock<IMongoCollection<Counter>> _countersMock;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _usersMock = new Mock<IMongoCollection<User>>();
        _countersMock = new Mock<IMongoCollection<Counter>>();
        _repository = new UserRepository(_usersMock.Object, _countersMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange - Configura usuário existente no mock
        var userId = 1;
        var expectedUser = new User { UserId = userId, Name = "Test User", Email = "test@example.com" };
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { expectedUser });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _usersMock.Setup(u => u.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por ID
        var result = await _repository.GetByIdAsync(userId);

        // Assert - Verifica se usuário correto foi retornado
        Assert.NotNull(result);
        Assert.Equal(expectedUser.UserId, result.UserId);
        Assert.Equal(expectedUser.Name, result.Name);
        Assert.Equal(expectedUser.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange - Configura mock para usuário inexistente
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _usersMock.Setup(u => u.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por ID inexistente
        var result = await _repository.GetByIdAsync(999);

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange - Configura usuário existente por email
        var email = "test@example.com";
        var expectedUser = new User { UserId = 1, Name = "Test User", Email = email };
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { expectedUser });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _usersMock.Setup(u => u.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por email
        var result = await _repository.GetByEmailAsync(email);

        // Assert - Verifica se usuário correto foi retornado
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Email, result.Email);
        Assert.Equal(expectedUser.Name, result.Name);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange - Configura mock para email inexistente
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _usersMock.Setup(u => u.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por email inexistente
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange - Configura lista de usuários para retorno
        var users = new List<User>
        {
            new User { UserId = 1, Name = "User 1", Email = "user1@example.com" },
            new User { UserId = 2, Name = "User 2", Email = "user2@example.com" }
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(users);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _usersMock.Setup(u => u.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de todos os usuários
        var result = await _repository.GetAllAsync();

        // Assert - Verifica se todos os usuários foram retornados
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(users[0].UserId, result[0].UserId);
        Assert.Equal(users[1].UserId, result[1].UserId);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_ShouldCallInsertOne()
    {
        // Arrange - Configura novo usuário para criação
        var user = new User { UserId = 1, Name = "New User", Email = "new@example.com" };

        // Act - Executa criação do usuário
        await _repository.CreateUserAsync(user);

        // Assert - Verifica se InsertOneAsync foi chamado corretamente
        _usersMock.Verify(u => u.InsertOneAsync(user, null, default), Times.Once);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_ShouldCallUpdateOne()
    {
        // Arrange - Configura dados para atualização de usuário
        var userId = 1;
        var user = new User
        {
            Name = "Updated User",
            Email = "updated@example.com",
            Status = true,
            Role = "Cuidador",
            Phone = "123456789",
            PasswordHash = "hashedpassword",
            LastLogin = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Executa atualização do usuário
        await _repository.UpdateUserAsync(userId, user);

        // Assert - Verifica se UpdateOneAsync foi chamado corretamente
        _usersMock.Verify(u => u.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ShouldCallDeleteOne()
    {
        // Arrange - Configura ID do usuário para exclusão
        var userId = 1;

        // Act - Executa exclusão do usuário
        await _repository.DeleteUserAsync(userId);

        // Assert - Verifica se DeleteOneAsync foi chamado corretamente
        _usersMock.Verify(u => u.DeleteOneAsync(It.IsAny<FilterDefinition<User>>(), default), Times.Once);
    }

    #endregion

    #region GetNextUserIdAsync Tests

    [Fact]
    public async Task GetNextUserIdAsync_ShouldReturnNextId()
    {
        // Arrange - Configura contador para geração de próximo ID
        var counter = new Counter { Id = "user", Seq = 5 };

        _countersMock.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Counter>>(),
            It.IsAny<UpdateDefinition<Counter>>(),
            It.IsAny<FindOneAndUpdateOptions<Counter, Counter>>(),
            default))
            .ReturnsAsync(counter);

        // Act - Executa obtenção do próximo ID de usuário
        var result = await _repository.GetNextUserIdAsync();

        // Assert - Verifica se o próximo ID foi retornado corretamente
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetNextUserIdAsync_ShouldReturnOne_WhenCounterNotExists()
    {
        // Arrange - Configura contador nulo (primeira vez)
        _countersMock.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Counter>>(),
            It.IsAny<UpdateDefinition<Counter>>(),
            It.IsAny<FindOneAndUpdateOptions<Counter, Counter>>(),
            default))
            .ReturnsAsync((Counter?)null);

        // Act - Executa obtenção do próximo ID quando contador não existe
        var result = await _repository.GetNextUserIdAsync();

        // Assert - Verifica se retorna 1 como padrão
        Assert.Equal(1, result);
    }

    #endregion

    #region UpdatePatientNameAsync Tests

    [Fact]
    public async Task UpdatePatientNameAsync_ShouldCallUpdateOne()
    {
        // Arrange - Configura dados para atualização do nome do paciente
        var userId = 1;
        var patientName = "Updated Patient Name";

        // Act - Executa atualização do nome do paciente
        await _repository.UpdatePatientNameAsync(userId, patientName);

        // Assert - Verifica se UpdateOneAsync foi chamado corretamente
        _usersMock.Verify(u => u.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    #endregion
}