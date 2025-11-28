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
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<EmotionMappingController>> _loggerMock;
    private readonly EmotionMappingController _controller;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public EmotionMappingControllerTests()
    {
        _mockEmotionMappingService = new Mock<IEmotionMappingService>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<EmotionMappingController>>();

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

        _controller = new EmotionMappingController(_mockEmotionMappingService.Object, _loggerMock.Object);
    }

    #region CreateMapping Tests

    [Fact]
    public async Task CreateMapping_WithValidData_ShouldReturnCreated()
    {
        // Arrange - Configurar usuário regular e dados válidos
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = EmotionMappingFixtures.CreateValidEmotionMappingRequest();
        var expectedResponse = EmotionMappingFixtures.CreateValidEmotionMappingResponse(null, userId);
        _mockEmotionMappingService.Setup(s => s.CreateMappingAsync(request, userId, "Cuidador")).ReturnsAsync(expectedResponse);

        // Act - Executar método CreateMapping
        var result = await _controller.CreateMapping(request);

        // Assert - Verificar se retornou Created com mapeamento criado
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(EmotionMappingController.GetMappingsByUser));
        createdResult.RouteValues.Should().ContainKey("userId").WhoseValue.Should().Be(userId);

        var response = createdResult.Value.Should().BeOfType<EmotionMappingResponse>().Subject;
        response.Emotion.Should().Be("happy");
        response.Message.Should().Be("Paciente está feliz!");
    }

    [Fact]
    public async Task CreateMapping_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = new EmotionMappingRequest();
        _mockEmotionMappingService.Setup(s => s.CreateMappingAsync(request, userId, "Cuidador"))
            .ThrowsAsync(new AppException("Dados inválidos"));

        // Act - Executar método CreateMapping
        var result = await _controller.CreateMapping(request);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Dados inválidos", response["message"]!.ToString());
    }

    [Fact]
    public async Task CreateMapping_WithUserIdZero_ShouldUseCurrentUserId()
    {
        // Arrange - Configurar usuário regular e request com UserId = null
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = EmotionMappingFixtures.CreateValidEmotionMappingRequest();
        request.UserId = null; // Simular UserId nulo, deve usar o ID do usuário atual

        var expectedResponse = EmotionMappingFixtures.CreateValidEmotionMappingResponse(null, userId);
        _mockEmotionMappingService.Setup(s => s.CreateMappingAsync(It.Is<EmotionMappingRequest>(r => r.UserId == userId), userId, "Cuidador"))
            .ReturnsAsync(expectedResponse);

        // Act - Executar método CreateMapping
        var result = await _controller.CreateMapping(request);

        // Assert - Verificar se UserId foi definido como o ID do usuário atual
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<EmotionMappingResponse>().Subject;
        response.UserId.Should().Be(userId);
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
        var adminId = _adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var targetUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var mappings = new List<EmotionMappingResponse>
        {
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping1", targetUserId),
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping2", targetUserId)
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(targetUserId, adminId, "Admin")).ReturnsAsync(mappings);

        // Act - Executar método GetMappingsByUser
        var result = await _controller.GetMappingsByUser(targetUserId);

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
        var adminId = _adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var invalidUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(invalidUserId, adminId, "Admin"))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método GetMappingsByUser
        var result = await _controller.GetMappingsByUser(invalidUserId);

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
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var mappings = new List<EmotionMappingResponse>
        {
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping1", userId),
            EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping2", userId)
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(userId, userId, "Cuidador")).ReturnsAsync(mappings);

        // Act - Executar método GetMyMappings
        var result = await _controller.GetMyMappings();

        // Assert - Verificar se retornou Ok com mapeamentos do usuário
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseMappings = okResult.Value.Should().BeAssignableTo<IEnumerable<EmotionMappingResponse>>().Subject;
        responseMappings.Should().HaveCount(2);
        responseMappings.All(m => m.UserId == userId).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyMappings_WithInvalidUser_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        _mockEmotionMappingService.Setup(s => s.GetMappingsByUserAsync(userId, userId, "Cuidador"))
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
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = EmotionMappingFixtures.CreateValidEmotionMappingRequest();
        request.Message = "Mensagem atualizada";
        var expectedResponse = EmotionMappingFixtures.CreateValidEmotionMappingResponse("mapping1", userId);
        expectedResponse.Message = "Mensagem atualizada";

        _mockEmotionMappingService.Setup(s => s.UpdateMappingAsync("mapping1", request, userId, "Cuidador")).ReturnsAsync(expectedResponse);

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
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = new EmotionMappingRequest();
        _mockEmotionMappingService.Setup(s => s.UpdateMappingAsync("mapping1", request, userId, "Cuidador"))
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
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        _mockEmotionMappingService.Setup(s => s.DeleteMappingAsync("mapping1", userId, "Cuidador")).Returns(Task.CompletedTask);

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
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        _mockEmotionMappingService.Setup(s => s.DeleteMappingAsync("invalid-id", userId, "Cuidador"))
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