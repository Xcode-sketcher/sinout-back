// ============================================================
// 👶 TESTES DO PATIENTSERVICE - GESTÃO DE PACIENTES
// ============================================================
// Valida a lógica de negócio de CRUD de pacientes,
// incluindo validações de dados e regras de autorização.

using Xunit;
using Moq;
using FluentAssertions;
using APISinout.Services;
using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Unit.Services;

/// <summary>
/// Testes completos para PatientService
/// Cobertura: CRUD, Autorização, Validações
/// </summary>
public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _mockPatientRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly PatientService _patientService;

    public PatientServiceTests()
    {
        _mockPatientRepository = new Mock<IPatientRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _patientService = new PatientService(_mockPatientRepository.Object, _mockUserRepository.Object);
    }

    #region CreatePatient Tests

    [Fact]
    public async Task CreatePatientAsync_AsCuidador_ShouldCreateForSelf()
    {
        // Arrange
        var cuidadorId = 1;
        var request = PatientFixtures.CreateValidPatientRequest();
        
        _mockPatientRepository.Setup(x => x.GetNextPatientIdAsync()).ReturnsAsync(1);
        _mockPatientRepository.Setup(x => x.CreatePatientAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(UserFixtures.CreateValidUser(cuidadorId));

        // Act
        var result = await _patientService.CreatePatientAsync(request, cuidadorId, "Cuidador");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.CuidadorId.Should().Be(cuidadorId);
        
        _mockPatientRepository.Verify(x => x.CreatePatientAsync(It.Is<Patient>(p => 
            p.CuidadorId == cuidadorId &&
            p.Name == request.Name &&
            p.Status == true &&
            p.CreatedBy == "self"
        )), Times.Once);
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithCuidadorId_ShouldCreateForSpecifiedCuidador()
    {
        // Arrange
        var adminId = 100;
        var cuidadorId = 1;
        var request = PatientFixtures.CreateValidPatientRequest(cuidadorId);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetNextPatientIdAsync()).ReturnsAsync(1);
        _mockPatientRepository.Setup(x => x.CreatePatientAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act
        var result = await _patientService.CreatePatientAsync(request, adminId, "Admin");

        // Assert
        result.Should().NotBeNull();
        result.CuidadorId.Should().Be(cuidadorId);
        
        _mockPatientRepository.Verify(x => x.CreatePatientAsync(It.Is<Patient>(p => 
            p.CuidadorId == cuidadorId &&
            p.CreatedBy == $"admin_{adminId}"
        )), Times.Once);
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithoutCuidadorId_ShouldThrowAppException()
    {
        // Arrange
        var adminId = 100;
        var request = PatientFixtures.CreateValidPatientRequest(null);

        // Act
        var act = async () => await _patientService.CreatePatientAsync(request, adminId, "Admin");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Administrador deve especificar o cuidador");
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithInvalidCuidador_ShouldThrowAppException()
    {
        // Arrange
        var adminId = 100;
        var invalidCuidadorId = 999;
        var request = PatientFixtures.CreateValidPatientRequest(invalidCuidadorId);
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(invalidCuidadorId)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _patientService.CreatePatientAsync(request, adminId, "Admin");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Cuidador inválido");
    }

    [Fact]
    public async Task CreatePatientAsync_WithEmptyName_ShouldThrowAppException()
    {
        // Arrange
        var request = PatientFixtures.CreateValidPatientRequest();
        request.Name = "";

        // Act
        var act = async () => await _patientService.CreatePatientAsync(request, 1, "Cuidador");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Nome do paciente é obrigatório");
    }

    #endregion

    #region GetPatientById Tests

    [Fact]
    public async Task GetPatientByIdAsync_AsOwner_ShouldReturnPatient()
    {
        // Arrange
        var cuidadorId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, cuidadorId);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act
        var result = await _patientService.GetPatientByIdAsync(patient.Id, cuidadorId, "Cuidador");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(patient.Id);
        result.CuidadorName.Should().Be(cuidador.Name);
    }

    [Fact]
    public async Task GetPatientByIdAsync_AsAdmin_ShouldReturnAnyPatient()
    {
        // Arrange
        var adminId = 100;
        var cuidadorId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, cuidadorId);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act
        var result = await _patientService.GetPatientByIdAsync(patient.Id, adminId, "Admin");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(patient.Id);
    }

    [Fact]
    public async Task GetPatientByIdAsync_AsNonOwner_ShouldThrowAppException()
    {
        // Arrange
        var requestingUserId = 2;
        var patientOwnerId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, patientOwnerId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);

        // Act
        var act = async () => await _patientService.GetPatientByIdAsync(patient.Id, requestingUserId, "Cuidador");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Acesso negado");
    }

    #endregion

    #region GetPatientsByCuidador Tests

    [Fact]
    public async Task GetPatientsByCuidadorAsync_ShouldReturnAllPatientsForCuidador()
    {
        // Arrange
        var cuidadorId = 1;
        var patients = PatientFixtures.CreateMultiplePatients(cuidadorId, 3);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByCuidadorIdAsync(cuidadorId)).ReturnsAsync(patients);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act
        var result = await _patientService.GetPatientsByCuidadorAsync(cuidadorId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.CuidadorId.Should().Be(cuidadorId));
    }

    #endregion

    #region UpdatePatient Tests

    [Fact]
    public async Task UpdatePatientAsync_AsOwner_ShouldUpdatePatient()
    {
        // Arrange
        var cuidadorId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, cuidadorId);
        var request = PatientFixtures.CreateValidPatientRequest();
        request.Name = "Nome Atualizado";
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.UpdatePatientAsync(It.IsAny<int>(), It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act
        var result = await _patientService.UpdatePatientAsync(patient.Id, request, cuidadorId, "Cuidador");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Nome Atualizado");
    }

    #endregion

    #region DeletePatient Tests

    [Fact]
    public async Task DeletePatientAsync_AsOwner_ShouldDeletePatient()
    {
        // Arrange
        var cuidadorId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.DeletePatientAsync(patient.Id)).Returns(Task.CompletedTask);

        // Act
        await _patientService.DeletePatientAsync(patient.Id, cuidadorId, "Cuidador");

        // Assert
        _mockPatientRepository.Verify(x => x.DeletePatientAsync(patient.Id), Times.Once);
    }

    [Fact]
    public async Task GetAllPatientsAsync_ShouldReturnAllPatientsWithCuidadorNames()
    {
        // Arrange
        var patient1 = PatientFixtures.CreateValidPatient(1, 1);
        patient1.Name = "Patient 1";
        var patient2 = PatientFixtures.CreateValidPatient(2, 2);
        patient2.Name = "Patient 2";
        
        var patients = new List<Patient> { patient1, patient2 };
        
        var cuidador1 = UserFixtures.CreateValidUser(1);
        var cuidador2 = UserFixtures.CreateValidUser(2);
        
        _mockPatientRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(patients);
        _mockUserRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(cuidador1);
        _mockUserRepository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(cuidador2);

        // Act
        var result = await _patientService.GetAllPatientsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Patient 1");
        result[0].CuidadorName.Should().Be(cuidador1.Name);
        result[1].Name.Should().Be("Patient 2");
        result[1].CuidadorName.Should().Be(cuidador2.Name);
    }

    #endregion
}
