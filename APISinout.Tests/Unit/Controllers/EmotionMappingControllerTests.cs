// ============================================================
// 🧠 TESTES DO EMOTIONMAPPINGCONTROLLER - MAPEAMENTO DE EMOÇÕES
// ============================================================
// Valida os endpoints de mapeamento de emoções, incluindo
// criação, consulta, atualização e exclusão de regras.

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

public class EmotionMappingControllerTests
{
    private readonly Mock<IEmotionMappingService> _mockEmotionMappingService;
    private readonly EmotionMappingController _controller;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public EmotionMappingControllerTests()
    {
        _mockEmotionMappingService = new Mock<IEmotionMappingService>();

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

        _controller = new EmotionMappingController(_mockEmotionMappingService.Object);
    }

    #region CreateMapping Tests

    [Fact]
    public async Task CreateMapping_WithValidData_ShouldReturnCreated()
    {
        // Arrange - Configurar usuário regular e dados válidos
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = EmotionMappingFixtures.CreateValidEmotionMappingRequest();
        var expectedResponse = EmotionMappingFixtures.CreateValidEmotionMappingResponse(null, 2);
        _mockEmotionMappingService.Setup(s => s.CreateMappingAsync(request, 2, "Cuidador")).ReturnsAsync(expectedResponse);

        // Act - Executar método CreateMapping
        var result = await _controller.CreateMapping(request);

        // Assert - Verificar se retornou Created com mapeamento criado
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(EmotionMappingController.GetMappingsByUser));
        createdResult.RouteValues.Should().ContainKey("userId").WhoseValue.Should().Be(2);

        var response = createdResult.Value.Should().BeOfType<EmotionMappingResponse>().Subject;
        response.Emotion.Should().Be("happy");
        response.Message.Should().Be("Paciente está feliz!");
    }

    [Fact]
    public async Task CreateMapping_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = new EmotionMappingRequest();
        _mockEmotionMappingService.Setup(s => s.CreateMappingAsync(request, 2, "Cuidador"))
            .ThrowsAsync(new AppException("Dados inválidos"));

        // Act - Executar método CreateMapping
        var result = await _controller.CreateMapping(request);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Dados inválidos", response["message"]!.ToString());
    }

    [Fact]
    public async Task CreateMapping_WithoutAuthentication_ShouldReturnBadRequest()
    {
        // Arrange - Configurar contexto sem usuário autenticado
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = EmotionMappingFixtures.CreateValidEmotionMappingRequest();

        // Act - Executar método CreateMapping
        var result = await _controller.CreateMapping(request);

        // Assert - Verificar se retornou BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region GetMappingsByUser Tests

    [Fact]
    public async Task GetMappingsByUser_WithValidUserId_ShouldReturnMappings()
    {
        // Arrange - Configurar usuário admin e lista de mapeamentos mock
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var mappings = new List<EmotionMappingResponse>
        {
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping1", 1),
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping2", 1)
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(1, 1, "Admin")).ReturnsAsync(mappings);

        // Act - Executar método GetMappingsByUser
        var result = await _controller.GetMappingsByUser(1);

        // Assert - Verificar se retornou Ok com lista de mapeamentos
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseMappings = okResult.Value.Should().BeAssignableTo<IEnumerable<EmotionMappingResponse>>().Subject;
        responseMappings.Should().HaveCount(2);
        responseMappings.First().Emotion.Should().Be("happy");
    }

    [Fact]
    public async Task GetMappingsByUser_WithInvalidUserId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(999, 1, "Admin"))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método GetMappingsByUser
        var result = await _controller.GetMappingsByUser(999);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region GetMyMappings Tests

    [Fact]
    public async Task GetMyMappings_WithValidUser_ShouldReturnUserMappings()
    {
        // Arrange - Configurar usuário regular e seus mapeamentos
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var mappings = new List<EmotionMappingResponse>
        {
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping1", 2),
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping2", 2)
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(2, 2, "Cuidador")).ReturnsAsync(mappings);

        // Act - Executar método GetMyMappings
        var result = await _controller.GetMyMappings();

        // Assert - Verificar se retornou Ok com mapeamentos do usuário
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseMappings = okResult.Value.Should().BeAssignableTo<IEnumerable<EmotionMappingResponse>>().Subject;
        responseMappings.Should().HaveCount(2);
        responseMappings.All(m => m.UserId == 2).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyMappings_WithInvalidUser_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(2, 2, "Cuidador"))
            .ThrowsAsync(new AppException("Erro ao buscar mapeamentos"));

        // Act - Executar método GetMyMappings
        var result = await _controller.GetMyMappings();

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Erro ao buscar mapeamentos", response["message"]!.ToString());
    }

    #endregion

    #region UpdateMapping Tests

    [Fact]
    public async Task UpdateMapping_WithValidData_ShouldReturnSuccessResponse()
    {
        // Arrange - Configurar usuário regular e dados válidos
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = EmotionMappingFixtures.CreateValidEmotionMappingRequest();
        request.Message = "Mensagem atualizada";
        var expectedResponse = EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping1", 2);
        expectedResponse.Message = "Mensagem atualizada";

        _mockEmotionMappingService.Setup(s => s.UpdateMappingAsync("mapping1", request, 2, "Cuidador")).ReturnsAsync(expectedResponse);

        // Act - Executar método UpdateMapping
        var result = await _controller.UpdateMapping("mapping1", request);

        // Assert - Verificar se retornou Ok com mapeamento atualizado
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<EmotionMappingResponse>().Subject;
        response.Message.Should().Be("Mensagem atualizada");
        response.Id.Should().Be("mapping1");
    }

    [Fact]
    public async Task UpdateMapping_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = new EmotionMappingRequest();
        _mockEmotionMappingService.Setup(s => s.UpdateMappingAsync("mapping1", request, 2, "Cuidador"))
            .ThrowsAsync(new AppException("Dados inválidos"));

        // Act - Executar método UpdateMapping
        var result = await _controller.UpdateMapping("mapping1", request);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Dados inválidos", response["message"]!.ToString());
    }

    #endregion

    #region DeleteMapping Tests

    [Fact]
    public async Task DeleteMapping_WithValidId_ShouldReturnSuccessMessage()
    {
        // Arrange - Configurar usuário regular
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        _mockEmotionMappingService.Setup(s => s.DeleteMappingAsync("mapping1", 2, "Cuidador")).Returns(Task.CompletedTask);

        // Act - Executar método DeleteMapping
        var result = await _controller.DeleteMapping("mapping1");

        // Assert - Verificar se retornou Ok com mensagem de sucesso
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = JObject.FromObject(okResult.Value!);
        Assert.Equal("Mapeamento removido com sucesso", response["message"]!.ToString());
    }

    [Fact]
    public async Task DeleteMapping_WithInvalidId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        _mockEmotionMappingService.Setup(s => s.DeleteMappingAsync("invalid-id", 2, "Cuidador"))
            .ThrowsAsync(new AppException("Mapeamento não encontrado"));

        // Act - Executar método DeleteMapping
        var result = await _controller.DeleteMapping("invalid-id");

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Mapeamento não encontrado", response["message"]!.ToString());
    }

    #endregion
}