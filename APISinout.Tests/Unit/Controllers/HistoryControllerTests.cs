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
using APISinout.Data;

namespace APISinout.Tests.Unit.Controllers;

public class HistoryControllerTests
{
    private readonly Mock<IHistoryService> _mockHistoryService;
    private readonly Mock<IPatientRepository> _mockPatientRepository;
    private readonly Mock<IEmotionMappingService> _mockEmotionMappingService;
    private readonly HistoryController _controller;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public HistoryControllerTests()
    {
        _mockHistoryService = new Mock<IHistoryService>();
        _mockPatientRepository = new Mock<IPatientRepository>();
        _mockEmotionMappingService = new Mock<IEmotionMappingService>();

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

        _controller = new HistoryController(_mockHistoryService.Object, _mockPatientRepository.Object, _mockEmotionMappingService.Object);
    }

    #region GetHistoryByPatient Tests

    [Fact]
    public async Task GetHistoryByPatient_WithValidPatientId_ShouldReturnHistory()
    {
        // Arrange - Configurar usuário admin e lista de histórico mock
        var adminId = _adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var history = new List<HistoryRecordResponse>
        {
            HistoryFixtures.CreateValidHistoryRecordResponse("hist1", null, patientId),
            HistoryFixtures.CreateValidHistoryRecordResponse("hist2", null, patientId)
        };

        _mockHistoryService.Setup(s => s.GetHistoryByPatientAsync(patientId, adminId, "Admin", 24)).ReturnsAsync(history);

        // Act - Executar método GetHistoryByPatient
        var result = await _controller.GetHistoryByPatient(patientId, 24);

        // Assert - Verificar se retornou Ok com lista de histórico
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseHistory = okResult.Value.Should().BeAssignableTo<IEnumerable<HistoryRecordResponse>>().Subject;
        responseHistory.Should().HaveCount(2);
        responseHistory.First().DominantEmotion.Should().Be("happy");
    }

    [Fact]
    public async Task GetHistoryByPatient_WithInvalidPatientId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        var adminId = _adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockHistoryService.Setup(s => s.GetHistoryByPatientAsync(patientId, adminId, "Admin", 24))
            .ThrowsAsync(new AppException("Paciente não encontrado"));

        // Act - Executar método GetHistoryByPatient
        var result = await _controller.GetHistoryByPatient(patientId, 24);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Paciente não encontrado", response["message"]!.ToString());
    }

    #endregion

    #region GetMyHistory Tests

    [Fact]
    public async Task GetMyHistory_WithValidUser_ShouldReturnUserHistory()
    {
        // Arrange - Configurar usuário regular e seu histórico
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var history = new List<HistoryRecordResponse>
        {
            HistoryFixtures.CreateValidHistoryRecordResponse("hist1", null, patientId),
            HistoryFixtures.CreateValidHistoryRecordResponse("hist2", null, patientId)
        };

        _mockHistoryService.Setup(s => s.GetHistoryByFilterAsync(It.IsAny<HistoryFilter>(), userId, "Cuidador")).ReturnsAsync(history);

        // Act - Executar método GetMyHistory
        var result = await _controller.GetMyHistory(24);

        // Assert - Verificar se retornou Ok com histórico do usuário
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseHistory = okResult.Value.Should().BeAssignableTo<IEnumerable<HistoryRecordResponse>>().Subject;
        responseHistory.Should().HaveCount(2);
        responseHistory.All(h => h.PatientId == patientId).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyHistory_WithEmptyHistory_ShouldReturnEmptyList()
    {
        // Arrange - Configurar usuário regular com histórico vazio
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var emptyHistory = new List<HistoryRecordResponse>();
        _mockHistoryService.Setup(s => s.GetHistoryByFilterAsync(It.IsAny<HistoryFilter>(), userId, "Cuidador")).ReturnsAsync(emptyHistory);

        // Act - Executar método GetMyHistory
        var result = await _controller.GetMyHistory(24);

        // Assert - Verificar se retornou Ok com lista vazia
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseHistory = okResult.Value.Should().BeAssignableTo<IEnumerable<HistoryRecordResponse>>().Subject;
        responseHistory.Should().BeEmpty();
    }

    #endregion

    #region GetHistoryByFilter Tests

    [Fact]
    public async Task GetHistoryByFilter_WithValidFilter_ShouldReturnFilteredHistory()
    {
        // Arrange - Configurar usuário regular e filtro
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var filter = HistoryFixtures.CreateValidHistoryFilter();
        var history = new List<HistoryRecordResponse>
        {
            HistoryFixtures.CreateValidHistoryRecordResponse("hist1", null, patientId)
        };

        _mockHistoryService.Setup(s => s.GetHistoryByFilterAsync(filter, userId, "Cuidador")).ReturnsAsync(history);

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
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var filter = new HistoryFilter();
        _mockHistoryService.Setup(s => s.GetHistoryByFilterAsync(filter, userId, "Cuidador"))
            .ThrowsAsync(new AppException("Filtro inválido"));

        // Act - Executar método GetHistoryByFilter
        var result = await _controller.GetHistoryByFilter(filter);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Filtro inválido", response["message"]!.ToString());
    }

    #endregion

    #region GetPatientStatistics Tests

    [Fact]
    public async Task GetPatientStatistics_WithValidPatientId_ShouldReturnStatistics()
    {
        // Arrange - Configurar usuário admin e estatísticas mock
        var adminId = _adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var stats = HistoryFixtures.CreateValidPatientStatistics(patientId);
        _mockHistoryService.Setup(s => s.GetPatientStatisticsAsync(patientId, adminId, "Admin", 24)).ReturnsAsync(stats);

        // Act - Executar método GetPatientStatistics
        var result = await _controller.GetPatientStatistics(patientId, 24);

        // Assert - Verificar se retornou Ok com estatísticas
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseStats = okResult.Value.Should().BeOfType<PatientStatistics>().Subject;
        responseStats.TotalAnalyses.Should().Be(10);
        responseStats.MostFrequentEmotion.Should().Be("happy");
    }

    [Fact]
    public async Task GetPatientStatistics_WithInvalidPatientId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário admin e serviço que lança exceção
        var adminId = _adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        _mockHistoryService.Setup(s => s.GetPatientStatisticsAsync(patientId, adminId, "Admin", 24))
            .ThrowsAsync(new AppException("Paciente não encontrado"));

        // Act - Executar método GetPatientStatistics
        var result = await _controller.GetPatientStatistics(patientId, 24);

        // Assert - Verificar se retornou BadRequest com mensagem de erro
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Paciente não encontrado", response["message"]!.ToString());
    }
    [Fact]
    public async Task SaveCuidadorEmotion_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange - Configurar usuário regular e requisição válida
        var userId = _regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = HistoryFixtures.CreateValidCuidadorEmotionRequest(userId);
        _mockHistoryService.Setup(s => s.CreateHistoryRecordAsync(It.IsAny<HistoryRecord>())).Returns(Task.CompletedTask);
        
        // Mock para PatientRepository
        var patient = new Patient { Id = patientId, Name = "Test Patient", CuidadorId = userId };
        _mockPatientRepository.Setup(r => r.GetByCuidadorIdAsync(userId)).ReturnsAsync(new List<Patient> { patient });

        // Configurar o serviço de mapeamento no HttpContext
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IEmotionMappingService))).Returns(_mockEmotionMappingService.Object);
        _controller.ControllerContext.HttpContext.RequestServices = serviceProviderMock.Object;

        _mockEmotionMappingService.Setup(s => s.FindMatchingRuleAsync(userId, "happy", 0.8))
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
        Assert.Equal("Request vazio ou formato inválido", response["message"]!.ToString());
    }

    [Fact]
    public async Task SaveCuidadorEmotion_WithInvalidCuidadorId_ShouldReturnBadRequest()
    {
        // Arrange - Configurar usuário regular
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = HistoryFixtures.CreateValidCuidadorEmotionRequest(""); // ID inválido

        // Act - Executar método SaveCuidadorEmotion
        var result = await _controller.SaveCuidadorEmotion(request);

        // Assert - Verificar se retornou BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Request vazio ou formato inválido", response["message"]!.ToString());
    }

    [Fact]
    public async Task SaveCuidadorEmotion_WithDifferentCuidadorId_ShouldReturnForbid()
    {
        // Arrange - Configurar usuário regular tentando salvar para outro cuidador
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var request = HistoryFixtures.CreateValidCuidadorEmotionRequest(MongoDB.Bson.ObjectId.GenerateNewId().ToString()); // ID diferente

        // Act - Executar método SaveCuidadorEmotion
        var result = await _controller.SaveCuidadorEmotion(request);

        // Assert - Verificar se retornou Forbid
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}