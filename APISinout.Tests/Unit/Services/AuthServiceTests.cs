// ============================================================
// 🔐 TESTES DO AUTHSERVICE - AUTENTICAÇÃO E REGISTRO
// ============================================================
// Valida a lógica de negócio de autenticação, registro de usuários,
// geração de tokens JWT e validação de credenciais.

using Xunit;
using Moq;
using FluentAssertions;
using APISinout.Services;
using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using APISinout.Tests.Fixtures;
using Microsoft.Extensions.Configuration;

namespace APISinout.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns("TestJwtKeyForUnitTestingPurposesOnlyNotForProductionUse123456789");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("SinoutAPI");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("SinoutClient");
        _mockConfiguration.Setup(x => x["Jwt:AccessTokenExpirationMinutes"]).Returns("60");

        _authService = new AuthService(_mockUserRepository.Object, _mockConfiguration.Object);
    }

    #region Register Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserSuccessfully()
    {
        // Arrange - Configura dados válidos e mocks para registro
        var request = UserFixtures.CreateValidRegisterRequest();
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(x => x.GetNextUserIdAsync()).ReturnsAsync(1);
        _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act - Executa registro do usuário
        var result = await _authService.RegisterAsync(request);

        // Assert - Verifica se usuário foi criado com dados corretos
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        Assert.Equal(request.Name, result.User!.Name);
        Assert.Equal(request.Email!.ToLower(), result.User!.Email);
        result.Token.Should().NotBeNullOrEmpty();
        
        _mockUserRepository.Verify(x => x.CreateUserAsync(It.Is<User>(u => 
            u.Name == request.Name &&
            u.Email == request.Email.ToLower().Trim() &&
            u.Status == true &&
            u.Role == "Cuidador"
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyEmail_ShouldThrowAppException()
    {
        // Arrange - Configura requisição com email vazio
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Email = "";

        // Act - Tenta executar registro com email inválido
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de dados inválidos
        await Assert.ThrowsAsync<AppException>(async () => await _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithShortPassword_ShouldThrowAppException()
    {
        // Arrange - Configura senha curta
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Password = "12345"; // Menos de 8 caracteres

        // Act - Tenta registrar com senha curta
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de senha muito curta
        await Assert.ThrowsAsync<AppException>(async () => await _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmail_ShouldThrowAppException()
    {
        // Arrange - Configura email inválido
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Email = "invalid-email"; // Email sem @

        // Act - Tenta registrar com email inválido
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de email inválido
        await Assert.ThrowsAsync<AppException>(async () => await _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidRole_ShouldThrowAppException()
    {
        // Arrange - Configura role inválido
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Role = "InvalidRole";

        // Act - Tenta registrar com role inválido
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de role inválido
        await Assert.ThrowsAsync<AppException>(async () => await _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithAdminRole_ShouldThrowAppException()
    {
        // Arrange - Tenta registrar como admin
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Role = UserRole.Admin.ToString();

        // Act - Tenta registrar como admin
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de não poder auto-registrar como admin
        await Assert.ThrowsAsync<AppException>(async () => await _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyName_ShouldThrowAppException()
    {
        // Arrange - Configura nome vazio
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Name = "";

        // Act - Tenta registrar com nome vazio
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de dados inválidos
        await Assert.ThrowsAsync<AppException>(async () => await _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyPassword_ShouldThrowAppException()
    {
        // Arrange - Configura senha vazia
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Password = "";

        // Act - Tenta registrar com senha vazia
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de dados inválidos
        await Assert.ThrowsAsync<AppException>(async () => await _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        // Arrange - Configura captura do usuário criado
        var request = UserFixtures.CreateValidRegisterRequest();
        User? capturedUser = null;
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(x => x.GetNextUserIdAsync()).ReturnsAsync(1);
        _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .Callback<User>(user => capturedUser = user)
            .Returns(Task.CompletedTask);

        // Act - Executa registro para capturar hash da senha
        await _authService.RegisterAsync(request);

        // Assert - Verifica se senha foi hasheada corretamente
        capturedUser.Should().NotBeNull();
        Assert.NotEqual(request.Password!, capturedUser!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(request.Password!, capturedUser!.PasswordHash!));
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange - Configura credenciais válidas e usuário existente
        var request = UserFixtures.CreateValidLoginRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<int>(), It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act - Executa login com credenciais válidas
        var result = await _authService.LoginAsync(request);

        // Assert - Verifica se resposta de autenticação foi retornada
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        Assert.Equal(user.Email!, result.User!.Email);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowAppException()
    {
        // Arrange - Configura senha incorreta
        var request = UserFixtures.CreateValidLoginRequest();
        request.Password = "WrongPassword123";
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);

        // Act - Tenta fazer login com senha errada
        var act = async () => await _authService.LoginAsync(request);

        // Assert - Deve lançar exceção de credenciais inválidas
        await Assert.ThrowsAsync<AppException>(async () => await _authService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithLockedUser_ShouldThrowAppException()
    {
        // Arrange - Configura usuário bloqueado
        var request = UserFixtures.CreateValidLoginRequest();
        var user = UserFixtures.CreateValidUser();
        user.LockoutEndDate = DateTime.UtcNow.AddMinutes(10); // Bloqueado por mais 10 minutos
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);

        // Act - Tenta fazer login com usuário bloqueado
        var act = async () => await _authService.LoginAsync(request);

        // Assert - Deve lançar exceção de conta bloqueada
        await Assert.ThrowsAsync<AppException>(async () => await _authService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithMultipleFailedAttempts_ShouldLockAccount()
    {
        // Arrange - Configura usuário com 4 tentativas falhidas
        var request = UserFixtures.CreateValidLoginRequest();
        request.Password = "WrongPassword123";
        var user = UserFixtures.CreateValidUser();
        user.FailedLoginAttempts = 4; // Uma tentativa a menos do limite
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);

        // Act - Faz login com senha errada (5ª tentativa)
        var act = async () => await _authService.LoginAsync(request);

        // Assert - Deve lançar exceção e bloquear conta
        await Assert.ThrowsAsync<AppException>(async () => await _authService.LoginAsync(request));
        
        // Verifica se UpdateUserAsync foi chamado para bloquear a conta
        _mockUserRepository.Verify(x => x.UpdateUserAsync(user.UserId, It.Is<User>(u => 
            u.LockoutEndDate.HasValue && 
            u.FailedLoginAttempts == 0)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithEmptyEmail_ShouldThrowAppException()
    {
        // Arrange - Configura email vazio
        var request = UserFixtures.CreateValidLoginRequest();
        request.Email = "";

        // Act - Tenta fazer login com email vazio
        var act = async () => await _authService.LoginAsync(request);

        // Assert - Deve lançar exceção de campos obrigatórios
        await Assert.ThrowsAsync<AppException>(async () => await _authService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ShouldThrowAppException()
    {
        // Arrange - Configura senha vazia
        var request = UserFixtures.CreateValidLoginRequest();
        request.Password = "";

        // Act - Tenta fazer login com senha vazia
        var act = async () => await _authService.LoginAsync(request);

        // Assert - Deve lançar exceção de campos obrigatórios
        await Assert.ThrowsAsync<AppException>(async () => await _authService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrowAppException()
    {
        // Arrange - Configura usuário inexistente
        var request = UserFixtures.CreateValidLoginRequest();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync((User?)null);

        // Act - Tenta fazer login com usuário inexistente
        var act = async () => await _authService.LoginAsync(request);

        // Assert - Deve lançar exceção de credenciais inválidas
        await Assert.ThrowsAsync<AppException>(async () => await _authService.LoginAsync(request));
    }

    #endregion
}
