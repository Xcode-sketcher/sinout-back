using Xunit;
using FluentAssertions;
using Moq;
using APISinout.Data;
using APISinout.Models;
using APISinout.Services;

namespace APISinout.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _service = new UserService(_userRepositoryMock.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange - Configura lista de usuários para retorno
        var users = new List<User>
        {
            new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Name = "User 1", Email = "user1@example.com", Role = "Admin" },
            new User { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Name = "User 2", Email = "user2@example.com", Role = "Cuidador" }
        };
        _userRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act - Executa busca de todos os usuários
        var result = await _service.GetAllAsync();

        // Assert - Verifica se todos os usuários foram retornados
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(users);
        _userRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyCollection()
    {
        // Arrange - Configura retorno de lista vazia
        _userRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

        // Act - Executa busca quando não há usuários
        var result = await _service.GetAllAsync();

        // Assert - Verifica se coleção vazia foi retornada
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange - Configura usuário existente para retorno
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            Role = "Cuidador"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act - Executa busca por ID existente
        var result = await _service.GetByIdAsync(userId);

        // Assert - Verifica se usuário correto foi retornado
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Name.Should().Be("Test User");
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ThrowsException()
    {
        // Arrange - Configura retorno nulo para usuário inexistente
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act - Tenta buscar usuário que não existe
        Func<Task> action = async () => await _service.GetByIdAsync("nonexistent-id");

        // Assert - Deve lançar exceção de usuário não encontrado
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Usuário não encontrado");
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Arrange - Configura usuário existente para retorno por email
        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "Test User",
            Email = "test@example.com",
            Role = "Cuidador"
        };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("test@example.com")).ReturnsAsync(user);

        // Act - Executa busca por email existente
        var result = await _service.GetByEmailAsync("test@example.com");

        // Assert - Verifica se usuário correto foi retornado
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistingEmail_ThrowsException()
    {
        // Arrange - Configura retorno nulo para email inexistente
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act - Tenta buscar usuário por email que não existe
        Func<Task> action = async () => await _service.GetByEmailAsync("nonexistent@example.com");

        // Assert - Deve lançar exceção de usuário não encontrado
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Usuário não encontrado");
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_ValidRequest_CreatesUser()
    {
        // Arrange - Configura requisição válida para criação de usuário
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "new@example.com",
            Password = "Password123",
            Role = "Cuidador"
        };
        _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Executa criação do usuário
        var result = await _service.CreateUserAsync(request, "admin");

        // Assert - Verifica se usuário foi criado com dados corretos
        result.Should().NotBeNull();
        result.Name.Should().Be("New User");
        result.Email.Should().Be("new@example.com");
        result.Role.Should().Be("Cuidador");
        result.Id.Should().NotBeNullOrEmpty();
        result.CreatedBy.Should().Be("admin");
        result.PasswordHash.Should().NotBeNullOrEmpty();
        _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_NoRoleSpecified_DefaultsToClient()
    {
        // Arrange - Configura requisição sem role especificada
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "new@example.com",
            Password = "Password123"
        };
        _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Cria usuário sem role
        var result = await _service.CreateUserAsync(request, "system");

        // Assert - Verifica se role padrão (Client) foi atribuída
        result.Role.Should().Be("Client");
    }

    [Fact]
    public async Task CreateUserAsync_PasswordIsHashed()
    {
        // Arrange - Configura requisição com senha em texto plano
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "new@example.com",
            Password = "PlainPassword123",
            Role = "Cuidador"
        };
        _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Cria usuário
        var result = await _service.CreateUserAsync(request, "admin");

        // Assert - Verifica se a senha foi hasheada corretamente
        result.PasswordHash.Should().NotBe("PlainPassword123");
        result.PasswordHash.Should().StartWith("$2a$"); // BCrypt hash prefix
        BCrypt.Net.BCrypt.Verify("PlainPassword123", result.PasswordHash).Should().BeTrue();
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_ValidRequest_UpdatesUser()
    {
        // Arrange - Configura usuário existente e dados para atualização
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var existingUser = new User
        {
            Id = userId,
            Name = "Old Name",
            Email = "old@example.com",
            Role = "Cuidador"
        };
        var updateRequest = new UpdateUserRequest
        {
            Name = "New Name",
            Email = "new@example.com",
            Role = "Admin"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.UpdateUserAsync(userId, It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Executa atualização completa do usuário
        await _service.UpdateUserAsync(userId, updateRequest);

        // Assert - Verifica se todos os campos foram atualizados
        _userRepositoryMock.Verify(x => x.UpdateUserAsync(userId, It.Is<User>(u =>
            u.Name == "New Name" &&
            u.Email == "new@example.com" &&
            u.Role == "Admin"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange - Configura atualização parcial de campos
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var existingUser = new User
        {
            Id = userId,
            Name = "Original Name",
            Email = "original@example.com",
            Role = "Cuidador"
        };
        var updateRequest = new UpdateUserRequest
        {
            Name = "Updated Name"
            // Outros campos null
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.UpdateUserAsync(userId, It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Executa atualização parcial
        await _service.UpdateUserAsync(userId, updateRequest);

        // Assert - Verifica se apenas os campos fornecidos foram alterados
        _userRepositoryMock.Verify(x => x.UpdateUserAsync(userId, It.Is<User>(u =>
            u.Name == "Updated Name" &&
            u.Email == "original@example.com" &&
            u.Role == "Cuidador"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistingUser_ThrowsException()
    {
        // Arrange - Configura atualização para usuário inexistente
        var updateRequest = new UpdateUserRequest { Name = "New Name" };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act - Tenta atualizar usuário que não existe
        Func<Task> action = async () => await _service.UpdateUserAsync("nonexistent-id", updateRequest);

        // Assert - Deve lançar exceção de usuário não encontrado
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Usuário não encontrado");
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_DeletesUser()
    {
        // Arrange - Configura usuário existente para exclusão
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var user = new User
        {
            Id = userId,
            Name = "User to Delete",
            Email = "delete@example.com"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.DeleteUserAsync(userId)).Returns(Task.CompletedTask);

        // Act - Executa exclusão do usuário
        await _service.DeleteUserAsync(userId);

        // Assert - Verifica se método de exclusão foi chamado
        _userRepositoryMock.Verify(x => x.DeleteUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistingUser_ThrowsException()
    {
        // Arrange - Configura exclusão para usuário inexistente
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act - Tenta excluir usuário que não existe
        Func<Task> action = async () => await _service.DeleteUserAsync("nonexistent-id");

        // Assert - Deve lançar exceção de usuário não encontrado
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Usuário não encontrado");
    }

    #endregion


}
