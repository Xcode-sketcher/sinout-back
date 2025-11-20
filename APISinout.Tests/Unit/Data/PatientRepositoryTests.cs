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

namespace APISinout.Tests.Unit.Data;

public class PatientRepositoryTests
{
    private readonly Mock<IMongoCollection<Patient>> _patientsMock;
    private readonly Mock<IMongoCollection<Counter>> _countersMock;
    private readonly PatientRepository _repository;

    public PatientRepositoryTests()
    {
        _patientsMock = new Mock<IMongoCollection<Patient>>();
        _countersMock = new Mock<IMongoCollection<Counter>>();
        _repository = new PatientRepository(_patientsMock.Object, _countersMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPatient_WhenExists()
    {
        // Arrange - Configura paciente existente no mock
        var patientId = 1;
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
        var result = await _repository.GetByIdAsync(999);

        // Assert - Verifica se null foi retornado
        Assert.Null(result);
    }

    #endregion

    #region GetByCuidadorIdAsync Tests

    [Fact]
    public async Task GetByCuidadorIdAsync_ShouldReturnPatients()
    {
        // Arrange - Configura pacientes associados a um cuidador
        var cuidadorId = 1;
        var patients = new List<Patient>
        {
            new Patient { Id = 1, CuidadorId = cuidadorId, Status = true },
            new Patient { Id = 2, CuidadorId = cuidadorId, Status = true }
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
        Assert.All(result, p => Assert.True(p.Status));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnActivePatients()
    {
        // Arrange - Configura pacientes ativos no mock
        var patients = new List<Patient>
        {
            new Patient { Id = 1, Status = true },
            new Patient { Id = 2, Status = true }
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
        Assert.All(result, p => Assert.True(p.Status));
    }

    #endregion

    #region CreatePatientAsync Tests

    [Fact]
    public async Task CreatePatientAsync_ShouldCallInsertOne()
    {
        // Arrange - Configura novo paciente para criação
        var patient = new Patient { Id = 1, Name = "New Patient" };

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
        var patientId = 1;
        var patient = new Patient { Name = "Updated Patient", CuidadorId = 2, Status = true };

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
    public async Task DeletePatientAsync_ShouldPerformSoftDelete()
    {
        // Arrange - Configura ID do paciente para exclusão lógica
        var patientId = 1;

        // Act - Executa exclusão lógica do paciente
        await _repository.DeletePatientAsync(patientId);

        // Assert - Verifica se UpdateOneAsync foi chamado para soft delete
        _patientsMock.Verify(p => p.UpdateOneAsync(
            It.IsAny<FilterDefinition<Patient>>(),
            It.IsAny<UpdateDefinition<Patient>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    #endregion

    #region GetNextPatientIdAsync Tests

    [Fact]
    public async Task GetNextPatientIdAsync_ShouldReturnNextId()
    {
        // Arrange - Configura contador para geração de próximo ID
        var counter = new Counter { Id = "patient", Seq = 5 };
        var mockCursor = new Mock<IAsyncCursor<Counter>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Counter> { counter });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _countersMock.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Counter>>(),
            It.IsAny<UpdateDefinition<Counter>>(),
            It.IsAny<FindOneAndUpdateOptions<Counter, Counter>>(),
            default))
            .ReturnsAsync(counter);

        // Act - Executa obtenção do próximo ID de paciente
        var result = await _repository.GetNextPatientIdAsync();

        // Assert - Verifica se o próximo ID foi retornado corretamente
        Assert.Equal(5, result);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenPatientExists()
    {
        // Arrange - Configura mock para paciente existente
        var patientId = 1;
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
        var patientId = 999;
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