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
    private readonly UserController _controller;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public UserControllerTests()
    {
        _mockUserService = new Mock<IUserService>();

        // Configurar usuário admin para testes
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.Role, "Admin")
        }));

        // Configurar usuário regular para testes
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Email, "user@test.com"),
            new Claim(ClaimTypes.Role, "Cuidador")
        }));

        _controller = new UserController(_mockUserService.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithAdminRole_ShouldReturnAllUsers()
    {
        // Arrange - Configurar usuário admin e lista de usuários mock
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var users = new List<User>
        {
            UserFixtures.CreateAdminUser(1),
            UserFixtures.CreateValidUser(2)
        };

        _mockUserService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        // Act - Executar método GetAll
        var result = await _controller.GetAll();

        // Assert - Verificar se retornou Ok com lista de usuários
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var userResponses = okResult.Value.Should().BeAssignableTo<IEnumerable<UserResponse>>().Subject;
        userResponses.Should().HaveCount(2);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreatedUser()
    {
        // Arrange - Configurar usuário admin e dados válidos
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var request = new CreateUserRequest
        {
            Name = "Novo Usuário",
            Email = "novo@test.com",
            Password = "Senha123!",
            Role = UserRole.Cuidador.ToString()
        };

        var createdUser = UserFixtures.CreateValidUser(3);
        createdUser.Email = request.Email;
        _mockUserService.Setup(s => s.CreateUserAsync(request, "admin@test.com")).ReturnsAsync(createdUser);

        // Act - Executar método Create
        var result = await _controller.Create(request);

        // Assert - Verificar se retornou Created com usuário criado
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UserController.GetById));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(createdUser.Id);

        var userResponse = createdResult.Value.Should().BeOfType<UserResponse>().Subject;
        userResponse.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Create_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var request = new CreateUserRequest();
        _mockUserService.Setup(s => s.CreateUserAsync(request, "admin@test.com"))
            .ThrowsAsync(new AppException("Dados inválidos"));

        // Act - Executar método Create
        var result = await _controller.Create(request);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Dados inválidos", response["message"]!.ToString());
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange - Configurar contexto sem usuário autenticado
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateUserRequest();

        // Act - Executar método Create
        var result = await _controller.Create(request);

        // Assert - Verificar se retornou Unauthorized
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_WithValidUser_ShouldReturnUserProfile()
    {
        // Arrange - Configurar usuário regular e perfil mock
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var user = UserFixtures.CreateValidUser(2);
        _mockUserService.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(user);

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

        _mockUserService.Setup(s => s.GetByIdAsync(2))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método GetCurrentUser
        var result = await _controller.GetCurrentUser();

        // Assert - Verificar se retornou NotFound com mensagem de erro
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = JObject.FromObject(notFoundResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnUser()
    {
        // Arrange - Configurar usuário admin e usuário mock
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var user = UserFixtures.CreateValidUser(1);
        _mockUserService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(user);

        // Act - Executar método GetById
        var result = await _controller.GetById(1);

        // Assert - Verificar se retornou Ok com usuário
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var userResponse = okResult.Value.Should().BeOfType<UserResponse>().Subject;
        userResponse.UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockUserService.Setup(s => s.GetByIdAsync(999))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método GetById
        var result = await _controller.GetById(999);

        // Assert - Verificar se retornou NotFound com mensagem de erro
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = JObject.FromObject(notFoundResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ShouldReturnSuccessMessage()
    {
        // Arrange - Configurar usuário admin e dados válidos
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var request = new UpdateUserRequest
        {
            Name = "Nome Atualizado",
            Email = "atualizado@test.com"
        };

        _mockUserService.Setup(s => s.UpdateUserAsync(1, request)).Returns(Task.CompletedTask);

        // Act - Executar método Update
        var result = await _controller.Update(1, request);

        // Assert - Verificar se retornou Ok com mensagem de sucesso
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = JObject.FromObject(okResult.Value!);
        Assert.Equal("Usuário atualizado com sucesso", response["message"]!.ToString());
    }

    [Fact]
    public async Task Update_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var request = new UpdateUserRequest();
        _mockUserService.Setup(s => s.UpdateUserAsync(1, request))
            .ThrowsAsync(new AppException("Dados inválidos"));

        // Act - Executar método Update
        var result = await _controller.Update(1, request);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Dados inválidos", response["message"]!.ToString());
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ShouldReturnSuccessMessage()
    {
        // Arrange - Configurar usuário admin
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockUserService.Setup(s => s.DeleteUserAsync(1)).Returns(Task.CompletedTask);

        // Act - Executar método Delete
        var result = await _controller.Delete(1);

        // Assert - Verificar se retornou Ok com mensagem de sucesso
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = JObject.FromObject(okResult.Value!);
        Assert.Equal("Usuário desativado com sucesso", response["message"]!.ToString());
    }

    [Fact]
    public async Task Delete_WithInvalidId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockUserService.Setup(s => s.DeleteUserAsync(1))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método Delete
        var result = await _controller.Delete(1);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region UpdatePatientName Tests

    [Fact]
    public async Task UpdatePatientName_WithValidData_ShouldReturnSuccessMessage()
    {
        // Arrange - Configurar usuário regular e dados válidos
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = new UpdatePatientNameRequest { PatientName = "Nome do Paciente" };
        _mockUserService.Setup(s => s.UpdatePatientNameAsync(2, request.PatientName)).Returns(Task.CompletedTask);

        // Act - Executar método UpdatePatientName
        var result = await _controller.UpdatePatientName(request);

        // Assert - Verificar se retornou Ok com mensagem de sucesso
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = JObject.FromObject(okResult.Value!);
        Assert.Equal("Nome do paciente atualizado com sucesso", response["message"]!.ToString());
    }

    [Fact]
    public async Task UpdatePatientName_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = new UpdatePatientNameRequest { PatientName = "Nome Inválido" };
        _mockUserService.Setup(s => s.UpdatePatientNameAsync(2, request.PatientName))
            .ThrowsAsync(new AppException("Nome inválido"));

        // Act - Executar método UpdatePatientName
        var result = await _controller.UpdatePatientName(request);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Nome inválido", response["message"]!.ToString());
    }

    #endregion

    #region GetAllCuidadores Tests

    [Fact]
    public async Task GetAllCuidadores_WithAdminRole_ShouldReturnOnlyCuidadores()
    {
        // Arrange - Configurar usuário admin e lista mista de usuários
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var users = new List<User>
        {
            UserFixtures.CreateAdminUser(1),
            UserFixtures.CreateValidUser(2),
            UserFixtures.CreateValidUser(3)
        };

        _mockUserService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        // Act - Executar método GetAllCuidadores
        var result = await _controller.GetAllCuidadores();

        // Assert - Verificar se retornou Ok apenas com cuidadores
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var cuidadorResponses = okResult.Value.Should().BeAssignableTo<IEnumerable<UserResponse>>().Subject;
        cuidadorResponses.Should().HaveCount(2);
        cuidadorResponses.All(c => c.Role == UserRole.Cuidador.ToString()).Should().BeTrue();
    }

    #endregion
}