// ============================================================
// 👥 TESTES DO USERSERVICE - GESTÃO DE USUÁRIOS
// ============================================================
// Valida a lógica de negócio de CRUD de usuários,
// incluindo validações e regras de autorização.

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
            new User { UserId = 1, Name = "User 1", Email = "user1@example.com", Role = "Admin" },
            new User { UserId = 2, Name = "User 2", Email = "user2@example.com", Role = "Cuidador" }
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
        var user = new User
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "Cuidador"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);

        // Act - Executa busca por ID existente
        var result = await _service.GetByIdAsync(1);

        // Assert - Verifica se usuário correto foi retornado
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Name.Should().Be("Test User");
        _userRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ThrowsException()
    {
        // Arrange - Configura retorno nulo para usuário inexistente
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        // Act - Tenta buscar usuário que não existe
        Func<Task> action = async () => await _service.GetByIdAsync(999);

        // Assert - Deve lançar exceção de usuário não encontrado
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("User not found");
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Arrange - Configura usuário existente para retorno por email
        var user = new User
        {
            UserId = 1,
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
            .WithMessage("User not found");
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
        _userRepositoryMock.Setup(x => x.GetNextUserIdAsync()).ReturnsAsync(5);
        _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Executa criação do usuário
        var result = await _service.CreateUserAsync(request, "admin");

        // Assert - Verifica se usuário foi criado com dados corretos
        result.Should().NotBeNull();
        result.Name.Should().Be("New User");
        result.Email.Should().Be("new@example.com");
        result.Role.Should().Be("Cuidador");
        result.UserId.Should().Be(5);
        result.Status.Should().BeTrue();
        result.CreatedBy.Should().Be("admin");
        result.PasswordHash.Should().NotBeNullOrEmpty();
        _userRepositoryMock.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_NoRoleSpecified_DefaultsToClient()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "new@example.com",
            Password = "Password123"
        };
        _userRepositoryMock.Setup(x => x.GetNextUserIdAsync()).ReturnsAsync(10);
        _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateUserAsync(request, "system");

        // Assert
        result.Role.Should().Be("Client");
    }

    [Fact]
    public async Task CreateUserAsync_PasswordIsHashed()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "new@example.com",
            Password = "PlainPassword123",
            Role = "Cuidador"
        };
        _userRepositoryMock.Setup(x => x.GetNextUserIdAsync()).ReturnsAsync(1);
        _userRepositoryMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateUserAsync(request, "admin");

        // Assert
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
        var existingUser = new User
        {
            UserId = 1,
            Name = "Old Name",
            Email = "old@example.com",
            Status = true,
            Role = "Cuidador"
        };
        var updateRequest = new UpdateUserRequest
        {
            Name = "New Name",
            Email = "new@example.com",
            Status = false,
            Role = "Admin"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.UpdateUserAsync(1, It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Executa atualização completa do usuário
        await _service.UpdateUserAsync(1, updateRequest);

        // Assert - Verifica se todos os campos foram atualizados
        _userRepositoryMock.Verify(x => x.UpdateUserAsync(1, It.Is<User>(u =>
            u.Name == "New Name" &&
            u.Email == "new@example.com" &&
            u.Status == false &&
            u.Role == "Admin"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var existingUser = new User
        {
            UserId = 1,
            Name = "Original Name",
            Email = "original@example.com",
            Status = true,
            Role = "Cuidador"
        };
        var updateRequest = new UpdateUserRequest
        {
            Name = "Updated Name"
            // Outros campos null
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.UpdateUserAsync(1, It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateUserAsync(1, updateRequest);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateUserAsync(1, It.Is<User>(u =>
            u.Name == "Updated Name" &&
            u.Email == "original@example.com" &&
            u.Status == true &&
            u.Role == "Cuidador"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistingUser_ThrowsException()
    {
        // Arrange
        var updateRequest = new UpdateUserRequest { Name = "New Name" };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        // Act
        Func<Task> action = async () => await _service.UpdateUserAsync(999, updateRequest);

        // Assert
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("User not found");
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_DeletesUser()
    {
        // Arrange - Configura usuário existente para exclusão
        var user = new User
        {
            UserId = 1,
            Name = "User to Delete",
            Email = "delete@example.com"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.DeleteUserAsync(1)).Returns(Task.CompletedTask);

        // Act - Executa exclusão do usuário
        await _service.DeleteUserAsync(1);

        // Assert - Verifica se método de exclusão foi chamado
        _userRepositoryMock.Verify(x => x.DeleteUserAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistingUser_ThrowsException()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        // Act
        Func<Task> action = async () => await _service.DeleteUserAsync(999);

        // Assert
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("User not found");
    }

    #endregion

    #region UpdatePatientNameAsync Tests

    [Fact]
    public async Task UpdatePatientNameAsync_ValidRequest_UpdatesPatientName()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Name = "Cuidador",
            Email = "cuidador@example.com",
            PatientName = "Old Patient"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdatePatientNameAsync(1, "New Patient")).Returns(Task.CompletedTask);

        // Act
        await _service.UpdatePatientNameAsync(1, "New Patient");

        // Assert
        _userRepositoryMock.Verify(x => x.UpdatePatientNameAsync(1, "New Patient"), Times.Once);
    }

    [Fact]
    public async Task UpdatePatientNameAsync_NonExistingUser_ThrowsException()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        // Act
        Func<Task> action = async () => await _service.UpdatePatientNameAsync(999, "Patient Name");

        // Assert
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("User not found");
    }

    #endregion
}
