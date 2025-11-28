// ============================================================
// 🔐 TESTES DO AUTHCONTROLLER - CONTROLADOR DE AUTENTICAÇÃO
// ============================================================
// Valida os endpoints de autenticação, registro, reset de senha
// e operações de usuário autenticado na API.

using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FluentValidation;
using System.Security.Claims;
using APISinout.Controllers;
using APISinout.Services;
using APISinout.Models;
using APISinout.Helpers;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace APISinout.Tests.Unit.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IPasswordResetService> _passwordResetServiceMock;
    private readonly Mock<IValidator<RegisterRequest>> _registerValidatorMock;
    private readonly Mock<IValidator<LoginRequest>> _loginValidatorMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IWebHostEnvironment> _webHostEnvironmentMock;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _passwordResetServiceMock = new Mock<IPasswordResetService>();
        _registerValidatorMock = new Mock<IValidator<RegisterRequest>>();
        _loginValidatorMock = new Mock<IValidator<LoginRequest>>();
        _configurationMock = new Mock<IConfiguration>();
        _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<AuthController>>();

        _controller = new AuthController(
            _authServiceMock.Object,
            _passwordResetServiceMock.Object,
            _registerValidatorMock.Object,
            _loginValidatorMock.Object,
            _configurationMock.Object,
            _webHostEnvironmentMock.Object,
            _loggerMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange - Configura requisição inválida com erros de validação
        var request = new RegisterRequest { Name = "", Email = "invalid-email" };
        var validationErrors = new List<FluentValidation.Results.ValidationFailure>
        {
            new FluentValidation.Results.ValidationFailure("Name", "Nome é obrigatório"),
            new FluentValidation.Results.ValidationFailure("Email", "Email inválido")
        };

        _registerValidatorMock.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationErrors));

        // Act - Executa registro com dados inválidos
        var result = await _controller.Register(request);

        // Assert - Verifica se retornou BadRequest com erros de validação
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<FluentValidation.Results.ValidationFailure>>(badRequestResult.Value);
        Assert.Equal(2, errors.Count());
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnBadRequest()
    {
        // Arrange - Configura requisição com email já existente
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "existing@example.com",
            Password = "password123"
        };

        _registerValidatorMock.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _authServiceMock.Setup(s => s.RegisterAsync(request))
            .ThrowsAsync(new AppException("Email já cadastrado"));

        // Act - Executa registro com email duplicado
        var result = await _controller.Register(request);

        // Assert - Verifica se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Email já cadastrado", response["message"]!.ToString());
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange - Configura credenciais inválidas
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        _loginValidatorMock.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _authServiceMock.Setup(s => s.LoginAsync(request))
            .ThrowsAsync(new AppException("Credenciais inválidas"));

        // Act - Executa login com credenciais erradas
        var result = await _controller.Login(request);

        // Assert - Verifica se retornou Unauthorized
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = JObject.FromObject(unauthorizedResult.Value!);
        Assert.Equal("Credenciais inválidas", response["message"]!.ToString());
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_WithValidRequest_ShouldReturnOk()
    {
        // Arrange - Configura requisição válida de reset de senha
        var request = new ForgotPasswordRequest { Email = "test@example.com" };
        var expectedResponse = new MessageResponse("Email enviado com sucesso");

        _passwordResetServiceMock.Setup(s => s.RequestPasswordResetAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act - Executa solicitação de reset de senha
        var result = await _controller.ForgotPassword(request);

        // Assert - Verifica se retornou Ok com resposta de sucesso
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MessageResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Message, response.Message);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange - Configura email inexistente
        var request = new ForgotPasswordRequest { Email = "nonexistent@example.com" };

        _passwordResetServiceMock.Setup(s => s.RequestPasswordResetAsync(request))
            .ThrowsAsync(new AppException("Email não encontrado"));

        // Act - Executa solicitação com email inválido
        var result = await _controller.ForgotPassword(request);

        // Assert - Verifica se retornou BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Email não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region ResendResetCode Tests

    [Fact]
    public async Task ResendResetCode_WithValidRequest_ShouldReturnOk()
    {
        // Arrange - Configura requisição válida para reenviar código
        var request = new ResendResetCodeRequest { Email = "test@example.com" };
        var expectedResponse = new MessageResponse("Código reenviado");

        _passwordResetServiceMock.Setup(s => s.ResendResetCodeAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act - Executa reenvio de código
        var result = await _controller.ResendResetCode(request);

        // Assert - Verifica se retornou Ok
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MessageResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Message, response.Message);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldReturnOk()
    {
        // Arrange - Configura reset de senha válido
        var request = new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "newpassword123"
        };
        var expectedResponse = new MessageResponse("Senha alterada com sucesso");

        _passwordResetServiceMock.Setup(s => s.ResetPasswordAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act - Executa reset de senha
        var result = await _controller.ResetPassword(request);

        // Assert - Verifica se retornou Ok
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MessageResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Message, response.Message);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WithValidRequest_ShouldReturnOk()
    {
        // Arrange - Configura mudança de senha para usuário autenticado
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123"
        };
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var expectedResponse = new MessageResponse("Senha alterada com sucesso");

        // Configura claims do usuário autenticado
        var claims = new List<Claim> { new Claim("userId", userId) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _passwordResetServiceMock.Setup(s => s.ChangePasswordAsync(request, userId))
            .ReturnsAsync(expectedResponse);

        // Act - Executa mudança de senha
        var result = await _controller.ChangePassword(request);

        // Assert - Verifica se retornou Ok
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MessageResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Message, response.Message);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_WithAuthenticatedUser_ShouldReturnUserInfo()
    {
        // Arrange - Configura usuário autenticado
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            Role = "Cuidador",
            Phone = "123456789"
        };

        // Configura claims do usuário autenticado
        var claims = new List<Claim> { new Claim("userId", userId) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _authServiceMock.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act - Executa obtenção de informações do usuário atual
        var result = await _controller.GetCurrentUser();

        // Assert - Verifica se retornou Ok com dados do usuário (anonymous type)
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        
        // Assert - Verifica propriedades usando reflection devido ao tipo anônimo
        var userIdProp = response.GetType().GetProperty("userId");
        var nameProp = response.GetType().GetProperty("name");
        Assert.NotNull(userIdProp);
        Assert.NotNull(nameProp);
        Assert.Equal(userId, userIdProp.GetValue(response));
        Assert.Equal("Test User", nameProp.GetValue(response));
    }

    [Fact]
    public async Task GetCurrentUser_WithUserNotFound_ShouldReturnNotFound()
    {
        // Arrange - Configura usuário não encontrado
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

        // Configura claims do usuário autenticado
        var claims = new List<Claim> { new Claim("userId", userId) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _authServiceMock.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act - Executa busca de usuário inexistente
        var result = await _controller.GetCurrentUser();

        // Assert - Verifica se retornou NotFound
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = JObject.FromObject(notFoundResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion
}