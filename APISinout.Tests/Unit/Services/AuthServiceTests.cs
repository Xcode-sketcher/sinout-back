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
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowAppException()
    {
        // Arrange - Configura usuário existente com mesmo email
        var request = UserFixtures.CreateValidRegisterRequest();
        var existingUser = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(existingUser);

        // Act - Tenta registrar com email duplicado
        var act = async () => await _authService.RegisterAsync(request);

        // Assert - Deve lançar exceção de email já cadastrado
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
    public async Task LoginAsync_WithInactiveUser_ShouldThrowAppException()
    {
        // Arrange - Configura usuário inativo
        var request = UserFixtures.CreateValidLoginRequest();
        var user = UserFixtures.CreateInactiveUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);

        // Act - Tenta fazer login com usuário inativo
        var act = async () => await _authService.LoginAsync(request);

        // Assert - Deve lançar exceção de credenciais inválidas
        await Assert.ThrowsAsync<AppException>(async () => await _authService.LoginAsync(request));
    }

    #endregion
}
