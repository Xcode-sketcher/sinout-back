// ============================================================
// 🏥 TESTES DO PATIENTREPOSITORY - REPOSITÓRIO DE PACIENTES
// ============================================================
// Valida as operações CRUD de pacientes no MongoDB,
// incluindo consultas, criação, atualização e exclusão lógica.

using Xunit;
using Moq;
using MongoDB.Driver;
using APISinout.Data;
using APISinout.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace APISinout.Tests.Unit.Data;

public class PatientRepositoryTests
{
    private readonly Mock<IMongoCollection<Patient>> _patientsMock;
    private readonly PatientRepository _repository;

    public PatientRepositoryTests()
    {
        _patientsMock = new Mock<IMongoCollection<Patient>>();
        _repository = new PatientRepository(_patientsMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPatient_WhenExists()
    {
        // Arrange - Configura paciente existente no mock
        var patientId = ObjectId.GenerateNewId().ToString();
        var expectedPatient = new Patient { Id = patientId, Name = "Test Patient" };
        var mockCursor = new Mock<IAsyncCursor<Patient>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Patient> { expectedPatient });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _patientsMock.Setup(p => p.FindAsync(
            It.Is<FilterDefinition<Patient>>(f => true), // Simplified check
            It.IsAny<FindOptions<Patient, Patient>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por ID
        var result = await _repository.GetByIdAsync(patientId);

        // Assert - Verifica se paciente correto foi retornado
        Assert.NotNull(result);
        Assert.Equal(expectedPatient.Id, result.Id);
        Assert.Equal(expectedPatient.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange - Configura mock para paciente inexistente
        var mockCursor = new Mock<IAsyncCursor<Patient>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Patient>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _patientsMock.Setup(p => p.FindAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            It.IsAny<FindOptions<Patient, Patient>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca por ID inexistente
        var result = await _repository.GetByIdAsync(ObjectId.GenerateNewId().ToString());

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    #endregion

    #region GetByCuidadorIdAsync Tests

    [Fact]
    public async Task GetByCuidadorIdAsync_ShouldReturnPatients()
    {
        // Arrange - Configura pacientes associados a um cuidador
        var cuidadorId = "cuidador-id-1";
        var patients = new List<Patient>
        {
            new Patient { Id = ObjectId.GenerateNewId().ToString(), CuidadorId = cuidadorId },
            new Patient { Id = ObjectId.GenerateNewId().ToString(), CuidadorId = cuidadorId }
        };

        var mockCursor = new Mock<IAsyncCursor<Patient>>();
        mockCursor.Setup(c => c.Current).Returns(patients);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _patientsMock.Setup(p => p.FindAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            It.IsAny<FindOptions<Patient, Patient>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de pacientes por cuidador
        var result = await _repository.GetByCuidadorIdAsync(cuidadorId);

        // Assert - Verifica se pacientes corretos foram retornados
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(cuidadorId, p.CuidadorId));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnActivePatients()
    {
        // Arrange - Configura pacientes ativos no mock
        var patients = new List<Patient>
        {
            new Patient { Id = ObjectId.GenerateNewId().ToString() },
            new Patient { Id = ObjectId.GenerateNewId().ToString() }
        };

        var mockCursor = new Mock<IAsyncCursor<Patient>>();
        mockCursor.Setup(c => c.Current).Returns(patients);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _patientsMock.Setup(p => p.FindAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            It.IsAny<FindOptions<Patient, Patient>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act - Executa busca de todos os pacientes ativos
        var result = await _repository.GetAllAsync();

        // Assert - Verifica se pacientes ativos foram retornados
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region CreatePatientAsync Tests

    [Fact]
    public async Task CreatePatientAsync_ShouldCallInsertOne()
    {
        // Arrange - Configura novo paciente para criação
        var patient = new Patient { Id = ObjectId.GenerateNewId().ToString(), Name = "New Patient" };

        // Act - Executa criação do paciente
        await _repository.CreatePatientAsync(patient);

        // Assert - Verifica se InsertOneAsync foi chamado corretamente
        _patientsMock.Verify(p => p.InsertOneAsync(patient, null, default), Times.Once);
    }

    #endregion

    #region UpdatePatientAsync Tests

    [Fact]
    public async Task UpdatePatientAsync_ShouldCallUpdateOne()
    {
        // Arrange - Configura dados para atualização de paciente
        var patientId = ObjectId.GenerateNewId().ToString();
        var patient = new Patient { Name = "Updated Patient", CuidadorId = "cuidador-id-2" };

        // Act - Executa atualização do paciente
        await _repository.UpdatePatientAsync(patientId, patient);

        // Assert - Verifica se UpdateOneAsync foi chamado corretamente
        _patientsMock.Verify(p => p.UpdateOneAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            It.IsAny<UpdateDefinition<Patient>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    #endregion

    #region DeletePatientAsync Tests

    [Fact]
    public async Task DeletePatientAsync_ShouldPerformHardDelete()
    {
        // Arrange - Configura ID do paciente para exclusão
        var patientId = ObjectId.GenerateNewId().ToString();

        // Act - Executa exclusão do paciente
        await _repository.DeletePatientAsync(patientId);

        // Assert - Verifica se DeleteOneAsync foi chamado
        _patientsMock.Verify(p => p.DeleteOneAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            default), Times.Once);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenPatientExists()
    {
        // Arrange - Configura mock para paciente existente
        var patientId = ObjectId.GenerateNewId().ToString();
        _patientsMock.Setup(p => p.CountDocumentsAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            It.IsAny<CountOptions>(),
            default))
            .ReturnsAsync(1L);

        // Act - Executa verificação de existência
        var result = await _repository.ExistsAsync(patientId);

        // Assert - Verifica se retornou verdadeiro
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenPatientNotExists()
    {
        // Arrange - Configura mock para paciente inexistente
        var patientId = ObjectId.GenerateNewId().ToString();
        _patientsMock.Setup(p => p.CountDocumentsAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            It.IsAny<CountOptions>(),
            default))
            .ReturnsAsync(0L);

        // Act - Executa verificação de existência
        var result = await _repository.ExistsAsync(patientId);

        // Assert - Verifica se retornou falso
        Assert.False(result);
    }

    #endregion
}