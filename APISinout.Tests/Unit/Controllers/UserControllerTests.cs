// ============================================================
// 👥 TESTES DO USERCONTROLLER - GERENCIAMENTO DE USUÁRIOS
// ============================================================
// Valida os endpoints de gerenciamento de usuários, incluindo
// criação, atualização, exclusão e consultas de usuários.

using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using APISinout.Controllers;
using APISinout.Services;
using APISinout.Models;
using APISinout.Helpers;
using APISinout.Tests.Fixtures;
using Newtonsoft.Json.Linq;

namespace APISinout.Tests.Unit.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IPatientService> _mockPatientService;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<UserController>> _loggerMock;
    private readonly UserController _controller;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public UserControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockPatientService = new Mock<IPatientService>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<UserController>>();

        // Configurar usuário admin para testes
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, MongoDB.Bson.ObjectId.GenerateNewId().ToString()),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.Role, "Admin")
        }));

        // Configurar usuário regular para testes
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, MongoDB.Bson.ObjectId.GenerateNewId().ToString()),
            new Claim(ClaimTypes.Email, "user@test.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        }));

        _controller = new UserController(_mockUserService.Object, _mockPatientService.Object, _loggerMock.Object);
    }

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_WithValidUser_ShouldReturnUserProfile()
    {
        // Arrange - Configurar usuário regular e perfil mock
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var user = UserFixtures.CreateValidUser();
        _mockUserService.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

        // Act - Executar método GetCurrentUser
        var result = await _controller.GetCurrentUser();

        // Assert - Verificar se retornou Ok com perfil do usuário
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var userResponse = okResult.Value.Should().BeOfType<UserResponse>().Subject;
        userResponse.Email.Should().Be("joao.silva@test.com");
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidUser_ShouldReturnNotFound()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        _mockUserService.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método GetCurrentUser
        var result = await _controller.GetCurrentUser();

        // Assert - Verificar se retornou NotFound com mensagem de erro
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = JObject.FromObject(notFoundResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion
}