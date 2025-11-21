// ============================================================
// 📊 TESTES DO HISTORYCONTROLLER - HISTÓRICO DE EMOÇÕES
// ============================================================
// Valida os endpoints de histórico de emoções, incluindo
// consultas, estatísticas e salvamento de emoções detectadas.

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

public class HistoryControllerTests
{
    private readonly Mock<IHistoryService> _mockHistoryService;
    private readonly Mock<IEmotionMappingService> _mockEmotionMappingService;
    private readonly HistoryController _controller;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public HistoryControllerTests()
    {
        _mockHistoryService = new Mock<IHistoryService>();
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

        _controller = new HistoryController(_mockHistoryService.Object);
    }

    #region GetHistoryByUser Tests

    [Fact]
    public async Task GetHistoryByUser_WithValidUserId_ShouldReturnHistory()
    {
        // Arrange - Configurar usuário admin e lista de histórico mock
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var history = new List<HistoryRecordResponse>
        {
            HistoryFixtures.CreateValidHistoryRecordResponse("hist1", 1),
            HistoryFixtures.CreateValidHistoryRecordResponse("hist2", 1)
        };

        _mockHistoryService.Setup(s => s.GetHistoryByUserAsync(1, 1, "Admin", 24)).ReturnsAsync(history);

        // Act - Executar método GetHistoryByUser
        var result = await _controller.GetHistoryByUser(1, 24);

        // Assert - Verificar se retornou Ok com lista de histórico
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseHistory = okResult.Value.Should().BeAssignableTo<IEnumerable<HistoryRecordResponse>>().Subject;
        responseHistory.Should().HaveCount(2);
        responseHistory.First().DominantEmotion.Should().Be("happy");
    }

    [Fact]
    public async Task GetHistoryByUser_WithInvalidUserId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockHistoryService.Setup(s => s.GetHistoryByUserAsync(999, 1, "Admin", 24))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método GetHistoryByUser
        var result = await _controller.GetHistoryByUser(999, 24);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region GetMyHistory Tests

    [Fact]
    public async Task GetMyHistory_WithValidUser_ShouldReturnUserHistory()
    {
        // Arrange - Configurar usuário regular e seu histórico
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var history = new List<HistoryRecordResponse>
        {
            HistoryFixtures.CreateValidHistoryRecordResponse("hist1", 2),
            HistoryFixtures.CreateValidHistoryRecordResponse("hist2", 2)
        };

        _mockHistoryService.Setup(s => s.GetHistoryByUserAsync(2, 2, "Cuidador", 24)).ReturnsAsync(history);

        // Act - Executar método GetMyHistory
        var result = await _controller.GetMyHistory(24);

        // Assert - Verificar se retornou Ok com histórico do usuário
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseHistory = okResult.Value.Should().BeAssignableTo<IEnumerable<HistoryRecordResponse>>().Subject;
        responseHistory.Should().HaveCount(2);
        responseHistory.All(h => h.PatientId == 2).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyHistory_WithEmptyHistory_ShouldReturnNotFound()
    {
        // Arrange - Configurar usuário regular com histórico vazio
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var emptyHistory = new List<HistoryRecordResponse>();
        _mockHistoryService.Setup(s => s.GetHistoryByUserAsync(2, 2, "Cuidador", 24)).ReturnsAsync(emptyHistory);

        // Act - Executar método GetMyHistory
        var result = await _controller.GetMyHistory(24);

        // Assert - Verificar se retornou NotFound
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Histórico não encontrado", notFoundResult.Value);
    }

    [Fact]
    public async Task GetMyHistory_WithLessThan24Hours_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var history = new List<HistoryRecordResponse>
        {
            HistoryFixtures.CreateValidHistoryRecordResponse("hist1", 2)
        };

        _mockHistoryService.Setup(s => s.GetHistoryByUserAsync(2, 2, "Cuidador", 12)).ReturnsAsync(history);

        // Act - Executar método GetMyHistory com menos de 24 horas
        var result = await _controller.GetMyHistory(12);

        // Assert - Verificar se retornou BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Histórico deve ter pelo menos 24 horas", badRequestResult.Value);
    }

    #endregion

    #region GetHistoryByFilter Tests

    [Fact]
    public async Task GetHistoryByFilter_WithValidFilter_ShouldReturnFilteredHistory()
    {
        // Arrange - Configurar usuário regular e filtro
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var filter = HistoryFixtures.CreateValidHistoryFilter();
        var history = new List<HistoryRecordResponse>
        {
            HistoryFixtures.CreateValidHistoryRecordResponse("hist1", 2)
        };

        _mockHistoryService.Setup(s => s.GetHistoryByFilterAsync(filter, 2, "Cuidador")).ReturnsAsync(history);

        // Act - Executar método GetHistoryByFilter
        var result = await _controller.GetHistoryByFilter(filter);

        // Assert - Verificar se retornou Ok com histórico filtrado
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseHistory = okResult.Value.Should().BeAssignableTo<IEnumerable<HistoryRecordResponse>>().Subject;
        responseHistory.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHistoryByFilter_WithInvalidFilter_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var filter = new HistoryFilter();
        _mockHistoryService.Setup(s => s.GetHistoryByFilterAsync(filter, 2, "Cuidador"))
            .ThrowsAsync(new AppException("Filtro inválido"));

        // Act - Executar método GetHistoryByFilter
        var result = await _controller.GetHistoryByFilter(filter);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Filtro inválido", response["message"]!.ToString());
    }

    #endregion

    #region GetUserStatistics Tests

    [Fact]
    public async Task GetUserStatistics_WithValidUserId_ShouldReturnStatistics()
    {
        // Arrange - Configurar usuário admin e estatísticas mock
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var stats = HistoryFixtures.CreateValidPatientStatistics(1);
        _mockHistoryService.Setup(s => s.GetUserStatisticsAsync(1, 1, "Admin", 24)).ReturnsAsync(stats);

        // Act - Executar método GetUserStatistics
        var result = await _controller.GetUserStatistics(1, 24);

        // Assert - Verificar se retornou Ok com estatísticas
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseStats = okResult.Value.Should().BeOfType<PatientStatistics>().Subject;
        responseStats.TotalAnalyses.Should().Be(10);
        responseStats.MostFrequentEmotion.Should().Be("happy");
    }

    [Fact]
    public async Task GetUserStatistics_WithInvalidUserId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockHistoryService.Setup(s => s.GetUserStatisticsAsync(999, 1, "Admin", 24))
            .ThrowsAsync(new AppException("Usuário não encontrado"));

        // Act - Executar método GetUserStatistics
        var result = await _controller.GetUserStatistics(999, 24);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Usuário não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region GetMyStatistics Tests

    [Fact]
    public async Task GetMyStatistics_WithValidUser_ShouldReturnUserStatistics()
    {
        // Arrange - Configurar usuário regular e suas estatísticas
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var stats = HistoryFixtures.CreateValidPatientStatistics(2);
        _mockHistoryService.Setup(s => s.GetUserStatisticsAsync(2, 2, "Cuidador", 24)).ReturnsAsync(stats);

        // Act - Executar método GetMyStatistics
        var result = await _controller.GetMyStatistics(24);

        // Assert - Verificar se retornou Ok com estatísticas do usuário
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseStats = okResult.Value.Should().BeOfType<PatientStatistics>().Subject;
        responseStats.PatientId.Should().Be(2);
        responseStats.TotalAnalyses.Should().Be(10);
    }

    [Fact]
    public async Task GetMyStatistics_WithNoAnalyses_ShouldReturnNotFound()
    {
        // Arrange - Configurar usuário regular com estatísticas vazias
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var emptyStats = HistoryFixtures.CreateValidPatientStatistics(2);
        emptyStats.TotalAnalyses = 0;
        _mockHistoryService.Setup(s => s.GetUserStatisticsAsync(2, 2, "Cuidador", 24)).ReturnsAsync(emptyStats);

        // Act - Executar método GetMyStatistics
        var result = await _controller.GetMyStatistics(24);

        // Assert - Verificar se retornou NotFound
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Estatísticas não encontradas", notFoundResult.Value);
    }

    [Fact]
    public async Task GetMyStatistics_WithLessThan24Hours_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var stats = HistoryFixtures.CreateValidPatientStatistics(2);
        _mockHistoryService.Setup(s => s.GetUserStatisticsAsync(2, 2, "Cuidador", 12)).ReturnsAsync(stats);

        // Act - Executar método GetMyStatistics com menos de 24 horas
        var result = await _controller.GetMyStatistics(12);

        // Assert - Verificar se retornou BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Estatísticas devem ter pelo menos 24 horas", badRequestResult.Value);
    }

    #endregion

    #region SaveCuidadorEmotion Tests

    [Fact]
    public async Task SaveCuidadorEmotion_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange - Configurar usuário regular e requisição válida
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = HistoryFixtures.CreateValidCuidadorEmotionRequest(2);
        _mockHistoryService.Setup(s => s.CreateHistoryRecordAsync(It.IsAny<HistoryRecord>())).Returns(Task.CompletedTask);

        // Configurar o serviço de mapeamento no HttpContext
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IEmotionMappingService))).Returns(_mockEmotionMappingService.Object);
        _controller.ControllerContext.HttpContext.RequestServices = serviceProviderMock.Object;

        _mockEmotionMappingService.Setup(s => s.FindMatchingRuleAsync(2, "happy", 0.8))
            .ReturnsAsync(("Mensagem encontrada", "rule1"));

        // Act - Executar método SaveCuidadorEmotion
        var result = await _controller.SaveCuidadorEmotion(request);

        // Assert - Verificar se retornou Ok com resposta de sucesso
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = JObject.FromObject(okResult.Value!);
        Assert.True((bool)response["sucesso"]!);
        Assert.Equal("Emoção registrada com sucesso", response["message"]!.ToString());
        Assert.Equal("Mensagem encontrada", response["suggestedMessage"]!.ToString());
    }

    [Fact]
    public async Task SaveCuidadorEmotion_WithNullRequest_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        // Act - Executar método SaveCuidadorEmotion com request nulo
        var result = await _controller.SaveCuidadorEmotion(null);

        // Assert - Verificar se retornou BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Request vazio ou formato inválido - verifique o JSON", response["message"]!.ToString());
    }

    [Fact]
    public async Task SaveCuidadorEmotion_WithInvalidCuidadorId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = HistoryFixtures.CreateValidCuidadorEmotionRequest(0); // ID inválido

        // Act - Executar método SaveCuidadorEmotion
        var result = await _controller.SaveCuidadorEmotion(request);

        // Assert - Verificar se retornou BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Request vazio ou formato inválido - verifique o JSON", response["message"]!.ToString());
    }

    [Fact]
    public async Task SaveCuidadorEmotion_WithDifferentCuidadorId_ShouldReturnForbid()
    {
        // Arrange - Configurar usuário regular tentando salvar para outro cuidador
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = HistoryFixtures.CreateValidCuidadorEmotionRequest(999); // ID diferente

        // Act - Executar método SaveCuidadorEmotion
        var result = await _controller.SaveCuidadorEmotion(request);

        // Assert - Verificar se retornou Forbid
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}