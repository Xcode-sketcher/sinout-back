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
    public async Task CreatePatientAsync_AsCaregiver_ShouldCreateForSelf()
    {
        // Arrange
        var caregiverId = 1;
        var request = PatientFixtures.CreateValidPatientRequest();
        
        _mockPatientRepository.Setup(x => x.GetNextPatientIdAsync()).ReturnsAsync(1);
        _mockPatientRepository.Setup(x => x.CreatePatientAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(caregiverId)).ReturnsAsync(UserFixtures.CreateValidUser(caregiverId));

        // Act
        var result = await _patientService.CreatePatientAsync(request, caregiverId, "Caregiver");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.CaregiverId.Should().Be(caregiverId);
        
        _mockPatientRepository.Verify(x => x.CreatePatientAsync(It.Is<Patient>(p => 
            p.CaregiverId == caregiverId &&
            p.Name == request.Name &&
            p.Status == true &&
            p.CreatedBy == "self"
        )), Times.Once);
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithCaregiverId_ShouldCreateForSpecifiedCaregiver()
    {
        // Arrange
        var adminId = 100;
        var caregiverId = 1;
        var request = PatientFixtures.CreateValidPatientRequest(caregiverId);
        var caregiver = UserFixtures.CreateValidUser(caregiverId);
        
        _mockPatientRepository.Setup(x => x.GetNextPatientIdAsync()).ReturnsAsync(1);
        _mockPatientRepository.Setup(x => x.CreatePatientAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(caregiverId)).ReturnsAsync(caregiver);

        // Act
        var result = await _patientService.CreatePatientAsync(request, adminId, "Admin");

        // Assert
        result.Should().NotBeNull();
        result.CaregiverId.Should().Be(caregiverId);
        
        _mockPatientRepository.Verify(x => x.CreatePatientAsync(It.Is<Patient>(p => 
            p.CaregiverId == caregiverId &&
            p.CreatedBy == $"admin_{adminId}"
        )), Times.Once);
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithoutCaregiverId_ShouldThrowAppException()
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
    public async Task CreatePatientAsync_AsAdmin_WithInvalidCaregiver_ShouldThrowAppException()
    {
        // Arrange
        var adminId = 100;
        var invalidCaregiverId = 999;
        var request = PatientFixtures.CreateValidPatientRequest(invalidCaregiverId);
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(invalidCaregiverId)).ReturnsAsync((User?)null);

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
        var act = async () => await _patientService.CreatePatientAsync(request, 1, "Caregiver");

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
        var caregiverId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, caregiverId);
        var caregiver = UserFixtures.CreateValidUser(caregiverId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockUserRepository.Setup(x => x.GetByIdAsync(caregiverId)).ReturnsAsync(caregiver);

        // Act
        var result = await _patientService.GetPatientByIdAsync(patient.Id, caregiverId, "Caregiver");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(patient.Id);
        result.CaregiverName.Should().Be(caregiver.Name);
    }

    [Fact]
    public async Task GetPatientByIdAsync_AsAdmin_ShouldReturnAnyPatient()
    {
        // Arrange
        var adminId = 100;
        var caregiverId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, caregiverId);
        var caregiver = UserFixtures.CreateValidUser(caregiverId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockUserRepository.Setup(x => x.GetByIdAsync(caregiverId)).ReturnsAsync(caregiver);

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
        var act = async () => await _patientService.GetPatientByIdAsync(patient.Id, requestingUserId, "Caregiver");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Acesso negado");
    }

    #endregion

    #region GetPatientsByCaregiver Tests

    [Fact]
    public async Task GetPatientsByCaregiverAsync_ShouldReturnAllPatientsForCaregiver()
    {
        // Arrange
        var caregiverId = 1;
        var patients = PatientFixtures.CreateMultiplePatients(caregiverId, 3);
        var caregiver = UserFixtures.CreateValidUser(caregiverId);
        
        _mockPatientRepository.Setup(x => x.GetByCaregiverIdAsync(caregiverId)).ReturnsAsync(patients);
        _mockUserRepository.Setup(x => x.GetByIdAsync(caregiverId)).ReturnsAsync(caregiver);

        // Act
        var result = await _patientService.GetPatientsByCaregiverAsync(caregiverId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.CaregiverId.Should().Be(caregiverId));
    }

    #endregion

    #region UpdatePatient Tests

    [Fact]
    public async Task UpdatePatientAsync_AsOwner_ShouldUpdatePatient()
    {
        // Arrange
        var caregiverId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, caregiverId);
        var request = PatientFixtures.CreateValidPatientRequest();
        request.Name = "Nome Atualizado";
        var caregiver = UserFixtures.CreateValidUser(caregiverId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.UpdatePatientAsync(It.IsAny<int>(), It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(caregiverId)).ReturnsAsync(caregiver);

        // Act
        var result = await _patientService.UpdatePatientAsync(patient.Id, request, caregiverId, "Caregiver");

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
        var caregiverId = 1;
        var patient = PatientFixtures.CreateValidPatient(1, caregiverId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.DeletePatientAsync(patient.Id)).Returns(Task.CompletedTask);

        // Act
        await _patientService.DeletePatientAsync(patient.Id, caregiverId, "Caregiver");

        // Assert
        _mockPatientRepository.Verify(x => x.DeletePatientAsync(patient.Id), Times.Once);
    }

    #endregion
}
