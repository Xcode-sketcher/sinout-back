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

        _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns("SuaChaveSecretaSuperSeguraParaJWT2024!MinimoDe32Caracteres");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("SinoutAPI");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("SinoutClient");
        _mockConfiguration.Setup(x => x["Jwt:AccessTokenExpirationMinutes"]).Returns("60");

        _authService = new AuthService(_mockUserRepository.Object, _mockConfiguration.Object);
    }

    #region Register Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(x => x.GetNextUserIdAsync()).ReturnsAsync(1);
        _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Name.Should().Be(request.Name);
        result.User.Email.Should().Be(request.Email.ToLower());
        result.Token.Should().NotBeNullOrEmpty();
        
        _mockUserRepository.Verify(x => x.CreateUserAsync(It.Is<User>(u => 
            u.Name == request.Name &&
            u.Email == request.Email.ToLower().Trim() &&
            u.Status == true &&
            u.Role == "Caregiver"
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyEmail_ShouldThrowAppException()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        request.Email = "";

        // Act
        var act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Dados inválidos");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowAppException()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        var existingUser = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(existingUser);

        // Act
        var act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Email já cadastrado");
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        // Arrange
        var request = UserFixtures.CreateValidRegisterRequest();
        User? capturedUser = null;
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(x => x.GetNextUserIdAsync()).ReturnsAsync(1);
        _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .Callback<User>(user => capturedUser = user)
            .Returns(Task.CompletedTask);

        // Act
        await _authService.RegisterAsync(request);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe(request.Password);
        BCrypt.Net.BCrypt.Verify(request.Password, capturedUser.PasswordHash).Should().BeTrue();
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = UserFixtures.CreateValidLoginRequest();
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<int>(), It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(user.Email);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowAppException()
    {
        // Arrange
        var request = UserFixtures.CreateValidLoginRequest();
        request.Password = "WrongPassword123";
        var user = UserFixtures.CreateValidUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Credenciais inválidas");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldThrowAppException()
    {
        // Arrange
        var request = UserFixtures.CreateValidLoginRequest();
        var user = UserFixtures.CreateInactiveUser();
        
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email.ToLower().Trim()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Credenciais inválidas");
    }

    #endregion
}
