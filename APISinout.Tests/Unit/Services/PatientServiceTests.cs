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
        // Arrange - Configura cuidador criando paciente para si mesmo
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = PatientFixtures.CreateValidPatientRequest();
        
        _mockPatientRepository.Setup(x => x.CreatePatientAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(UserFixtures.CreateValidUser(cuidadorId));

        // Act - Executa a ação sob teste
        var result = await _patientService.CreatePatientAsync(request, cuidadorId, "Cuidador");

        // Assert - Verifica o resultado esperado
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.CuidadorId.Should().Be(cuidadorId);
        
        _mockPatientRepository.Verify(x => x.CreatePatientAsync(It.Is<Patient>(p => 
            p.CuidadorId == cuidadorId &&
            p.Name == request.Name
        )), Times.Once);
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithCuidadorId_ShouldCreateForSpecifiedCuidador()
    {
        // Arrange - Configura admin criando paciente para cuidador específico
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = PatientFixtures.CreateValidPatientRequest(cuidadorId);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.CreatePatientAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act - Executa a ação sob teste
        var result = await _patientService.CreatePatientAsync(request, adminId, "Admin");

        // Assert - Verifica o resultado esperado
        result.Should().NotBeNull();
        result.CuidadorId.Should().Be(cuidadorId);
        
        _mockPatientRepository.Verify(x => x.CreatePatientAsync(It.Is<Patient>(p => 
            p.CuidadorId == cuidadorId
        )), Times.Once);
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithoutCuidadorId_ShouldThrowAppException()
    {
        // Arrange - Configura admin tentando criar paciente sem especificar cuidador
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = PatientFixtures.CreateValidPatientRequest(null);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.CreatePatientAsync(request, adminId, "Admin");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Administrador deve especificar o cuidador");
    }

    [Fact]
    public async Task CreatePatientAsync_AsAdmin_WithInvalidCuidador_ShouldThrowAppException()
    {
        // Arrange - Configura admin tentando criar paciente com cuidador inválido
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var invalidCuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = PatientFixtures.CreateValidPatientRequest(invalidCuidadorId);
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(invalidCuidadorId)).ReturnsAsync((User?)null);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.CreatePatientAsync(request, adminId, "Admin");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Cuidador inválido");
    }

    [Fact]
    public async Task CreatePatientAsync_WithEmptyName_ShouldThrowAppException()
    {
        // Arrange - Configura criação de paciente com nome vazio
        var request = PatientFixtures.CreateValidPatientRequest();
        request.Name = "";
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.CreatePatientAsync(request, userId, "Cuidador");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Nome do paciente é obrigatório");
    }

    [Fact]
    public async Task CreatePatientAsync_InvalidRole_ShouldThrowAppException()
    {
        // Arrange - Configura criação de paciente com role inválido
        var request = PatientFixtures.CreateValidPatientRequest();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        
        // Act - Executa a ação sob teste
        var act = async () => await _patientService.CreatePatientAsync(request, userId, "User");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Apenas Cuidadores podem cadastrar pacientes");
    }

    #endregion

    #region GetPatientById Tests

    [Fact]
    public async Task GetPatientByIdAsync_AsOwner_ShouldReturnPatient()
    {
        // Arrange - Configura cuidador dono buscando seu próprio paciente
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(null, cuidadorId);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id!)).ReturnsAsync(patient);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act - Executa a ação sob teste
        var result = await _patientService.GetPatientByIdAsync(patient.Id!, cuidadorId, "Cuidador");

        // Assert - Verifica o resultado esperado
        result.Should().NotBeNull();
        result.Id.Should().Be(patient.Id);
        result.CuidadorName.Should().Be(cuidador.Name);
    }

    [Fact]
    public async Task GetPatientByIdAsync_AsAdmin_ShouldReturnAnyPatient()
    {
        // Arrange - Configura admin buscando qualquer paciente
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(null, cuidadorId);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id!)).ReturnsAsync(patient);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act - Executa a ação sob teste
        var result = await _patientService.GetPatientByIdAsync(patient.Id!, adminId, "Admin");

        // Assert - Verifica o resultado esperado
        result.Should().NotBeNull();
        result.Id.Should().Be(patient.Id);
    }

    [Fact]
    public async Task GetPatientByIdAsync_AsNonOwner_ShouldThrowAppException()
    {
        // Arrange - Configura cuidador não dono tentando acessar paciente de outro
        var requestingUserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patientOwnerId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(null, patientOwnerId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id!)).ReturnsAsync(patient);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.GetPatientByIdAsync(patient.Id!, requestingUserId, "Cuidador");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Acesso negado");
    }

    [Fact]
    public async Task GetPatientByIdAsync_NotFound_ShouldThrowAppException()
    {
        // Arrange - Configura busca de paciente inexistente
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Patient?)null);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.GetPatientByIdAsync(id, userId, "Cuidador");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Paciente não encontrado");
    }

    #endregion

    #region GetPatientsByCuidador Tests

    [Fact]
    public async Task GetPatientsByCuidadorAsync_ShouldReturnAllPatientsForCuidador()
    {
        // Arrange - Configura busca de todos os pacientes de um cuidador
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patients = PatientFixtures.CreateMultiplePatients(cuidadorId, 3);
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByCuidadorIdAsync(cuidadorId)).ReturnsAsync(patients);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act - Executa a ação sob teste
        var result = await _patientService.GetPatientsByCuidadorAsync(cuidadorId);

        // Assert - Verifica o resultado esperado
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.CuidadorId.Should().Be(cuidadorId));
    }

    #endregion

    #region UpdatePatient Tests

    [Fact]
    public async Task UpdatePatientAsync_AsOwner_ShouldUpdatePatient()
    {
        // Arrange - Configura cuidador dono atualizando seu próprio paciente
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(null, cuidadorId);
        var request = PatientFixtures.CreateValidPatientRequest();
        request.Name = "Nome Atualizado";
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id!)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.UpdatePatientAsync(patient.Id!, It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act - Executa a ação sob teste
        var result = await _patientService.UpdatePatientAsync(patient.Id!, request, cuidadorId, "Cuidador");

        // Assert - Verifica o resultado esperado
        result.Should().NotBeNull();
        result.Name.Should().Be("Nome Atualizado");
    }

    [Fact]
    public async Task UpdatePatientAsync_NotFound_ShouldThrowAppException()
    {
        // Arrange - Configura tentativa de atualização de paciente inexistente
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var request = PatientFixtures.CreateValidPatientRequest();
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Patient?)null);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.UpdatePatientAsync(id, request, userId, "Cuidador");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Paciente não encontrado");
    }

    [Fact]
    public async Task UpdatePatientAsync_AccessDenied_ShouldThrowAppException()
    {
        // Arrange - Configura tentativa de atualização sem permissão de acesso
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var ownerId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var requesterId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(id, ownerId);
        var request = PatientFixtures.CreateValidPatientRequest();
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(patient);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.UpdatePatientAsync(id, request, requesterId, "Cuidador");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Acesso negado");
    }

    [Fact]
    public async Task UpdatePatientAsync_UpdateAllFields_ShouldUpdatePatient()
    {
        // Arrange - Configura atualização de todos os campos do paciente
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(id, cuidadorId);
        var request = new PatientRequest 
        { 
            Name = "New Name", 
            AdditionalInfo = "New Info", 
            ProfilePhoto = 1 
        };
        var cuidador = UserFixtures.CreateValidUser(cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.UpdatePatientAsync(id, It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidadorId)).ReturnsAsync(cuidador);

        // Act - Executa a ação sob teste
        var result = await _patientService.UpdatePatientAsync(id, request, cuidadorId, "Cuidador");

        // Assert - Verifica o resultado esperado
        result.Name.Should().Be("New Name");
        result.AdditionalInfo.Should().Be("New Info");
        result.ProfilePhoto.Should().Be(1);
    }

    [Fact]
    public async Task UpdatePatientAsync_AdminChangeCuidador_Success()
    {
        // Arrange - Configura admin alterando cuidador do paciente
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var oldCuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var newCuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(id, oldCuidadorId);
        var request = new PatientRequest { CuidadorId = newCuidadorId };
        var newCuidador = UserFixtures.CreateValidUser(newCuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.UpdatePatientAsync(id, It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.GetByIdAsync(newCuidadorId)).ReturnsAsync(newCuidador);

        // Act - Executa a ação sob teste
        var result = await _patientService.UpdatePatientAsync(id, request, adminId, "Admin");

        // Assert - Verifica o resultado esperado
        result.CuidadorId.Should().Be(newCuidadorId);
    }

    [Fact]
    public async Task UpdatePatientAsync_AdminChangeCuidador_InvalidCuidador_ThrowsException()
    {
        // Arrange - Configura admin tentando alterar para cuidador inválido
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var adminId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var invalidCuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(id, MongoDB.Bson.ObjectId.GenerateNewId().ToString());
        var request = new PatientRequest { CuidadorId = invalidCuidadorId };
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(patient);
        _mockUserRepository.Setup(x => x.GetByIdAsync(invalidCuidadorId)).ReturnsAsync((User?)null);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.UpdatePatientAsync(id, request, adminId, "Admin");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Cuidador inválido");
    }

    #endregion

    #region DeletePatient Tests

    [Fact]
    public async Task DeletePatientAsync_AsOwner_ShouldDeletePatient()
    {
        // Arrange - Configura cuidador dono excluindo seu próprio paciente
        var cuidadorId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(null, cuidadorId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(patient.Id!)).ReturnsAsync(patient);
        _mockPatientRepository.Setup(x => x.DeletePatientAsync(patient.Id!)).Returns(Task.CompletedTask);

        // Act - Executa a ação sob teste
        await _patientService.DeletePatientAsync(patient.Id!, cuidadorId, "Cuidador");

        // Assert - Verifica o resultado esperado
        _mockPatientRepository.Verify(x => x.DeletePatientAsync(patient.Id!), Times.Once);
    }

    [Fact]
    public async Task DeletePatientAsync_NotFound_ShouldThrowAppException()
    {
        // Arrange - Configura tentativa de exclusão de paciente inexistente
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var userId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Patient?)null);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.DeletePatientAsync(id, userId, "Cuidador");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Paciente não encontrado");
    }

    [Fact]
    public async Task DeletePatientAsync_AccessDenied_ShouldThrowAppException()
    {
        // Arrange - Configura tentativa de exclusão sem permissão de acesso
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var ownerId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var requesterId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient = PatientFixtures.CreateValidPatient(id, ownerId);
        
        _mockPatientRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(patient);

        // Act - Executa a ação sob teste
        var act = async () => await _patientService.DeletePatientAsync(id, requesterId, "Cuidador");

        // Assert - Verifica o resultado esperado
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("Acesso negado");
    }

    [Fact]
    public async Task GetAllPatientsAsync_ShouldReturnAllPatientsWithCuidadorNames()
    {
        // Arrange - Configura busca de todos os pacientes com nomes dos cuidadores
        var cuidador1Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var cuidador2Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var patient1 = PatientFixtures.CreateValidPatient(null, cuidador1Id);
        patient1.Name = "Patient 1";
        var patient2 = PatientFixtures.CreateValidPatient(null, cuidador2Id);
        patient2.Name = "Patient 2";
        
        var patients = new List<Patient> { patient1, patient2 };
        
        var cuidador1 = UserFixtures.CreateValidUser(cuidador1Id);
        var cuidador2 = UserFixtures.CreateValidUser(cuidador2Id);
        
        _mockPatientRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(patients);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidador1Id)).ReturnsAsync(cuidador1);
        _mockUserRepository.Setup(x => x.GetByIdAsync(cuidador2Id)).ReturnsAsync(cuidador2);

        // Act - Executa a ação sob teste
        var result = await _patientService.GetAllPatientsAsync();

        // Assert - Verifica o resultado esperado
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Patient 1");
        result[0].CuidadorName.Should().Be(cuidador1.Name);
        result[1].Name.Should().Be("Patient 2");
        result[1].CuidadorName.Should().Be(cuidador2.Name);
    }

    #endregion
}
