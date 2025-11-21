// ============================================================
// 🏥 TESTES DO PATIENTCONTROLLER - GERENCIAMENTO DE PACIENTES
// ============================================================
// Valida os endpoints de gerenciamento de pacientes, incluindo
// criação, consulta, atualização e exclusão de pacientes.

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

public class PatientControllerTests
{
    private readonly Mock<IPatientService> _mockPatientService;
    private readonly PatientController _controller;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public PatientControllerTests()
    {
        _mockPatientService = new Mock<IPatientService>();

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

        _controller = new PatientController(_mockPatientService.Object);
    }

    #region GetPatients Tests

    [Fact]
    public async Task GetPatients_WithAdminRole_ShouldReturnAllPatients()
    {
        // Arrange - Configurar usuário admin e lista de pacientes
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _adminUser }
        };

        var patients = new List<PatientResponse>
        {
            new PatientResponse(PatientFixtures.CreateValidPatient(1, 1)),
            new PatientResponse(PatientFixtures.CreateValidPatient(2, 2))
        };

        _mockPatientService.Setup(s => s.GetAllPatientsAsync()).ReturnsAsync(patients);

        // Act - Executar método GetPatients
        var result = await _controller.GetPatients();

        // Assert - Verificar se retornou Ok com todos os pacientes (caminho admin)
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responsePatients = okResult.Value.Should().BeAssignableTo<IEnumerable<PatientResponse>>().Subject;
        responsePatients.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPatients_WithRegularUserRole_ShouldReturnOwnPatients()
    {
        // Arrange - Configurar usuário regular e seus pacientes
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _regularUser }
        };

        var patients = new List<PatientResponse>
        {
            new PatientResponse(PatientFixtures.CreateValidPatient(1, 2)),
            new PatientResponse(PatientFixtures.CreateValidPatient(2, 2))
        };

        _mockPatientService.Setup(s => s.GetPatientsByCuidadorAsync(2)).ReturnsAsync(patients);

        // Act - Executar método GetPatients
        var result = await _controller.GetPatients();

        // Assert - Verificar se retornou Ok com pacientes do cuidador (caminho não-admin)
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responsePatients = okResult.Value.Should().BeAssignableTo<IEnumerable<PatientResponse>>().Subject;
        responsePatients.Should().HaveCount(2);
        responsePatients.All(p => p.CuidadorId == 2).Should().BeTrue();
    }

    #endregion
}