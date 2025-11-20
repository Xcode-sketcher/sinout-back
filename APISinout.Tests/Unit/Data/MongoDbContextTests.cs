// ============================================================
// 🗄️ TESTES DO MONGODBCONTEXT - CONTEXTO DO BANCO DE DADOS
// ============================================================
// Valida a inicialização e configuração das coleções MongoDB,
// garantindo que todas as coleções sejam acessíveis corretamente.

using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using APISinout.Data;
using APISinout.Models;

namespace APISinout.Tests.Unit.Data;

public class MongoDbContextTests
{
    private readonly Mock<IConfiguration> _configMock;

    public MongoDbContextTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["MongoDb:ConnectionString"]).Returns("mongodb://localhost:27017");
        _configMock.Setup(c => c["MongoDb:DatabaseName"]).Returns("testdb");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeDatabase()
    {
        // Arrange - Configura mock da configuração com dados válidos
        // Act - Instancia o contexto do MongoDB
        var context = new MongoDbContext(_configMock.Object);

        // Assert - Verifica se todas as coleções foram inicializadas corretamente
        Assert.NotNull(context);
        // Verificar se as coleções são acessíveis (não null)
        Assert.NotNull(context.Users);
        Assert.NotNull(context.Counters);
        Assert.NotNull(context.Patients);
        Assert.NotNull(context.EmotionMappings);
        Assert.NotNull(context.HistoryRecords);
        Assert.NotNull(context.PasswordResetTokens);
    }

    #endregion

    #region Collection Access Tests

    [Fact]
    public void UsersCollection_ShouldReturnCorrectCollection()
    {
        // Arrange - Instancia contexto com configuração mockada
        // Act - Acessa a coleção de usuários
        var context = new MongoDbContext(_configMock.Object);
        var collection = context.Users;

        // Assert - Verifica se a coleção correta foi retornada
        Assert.NotNull(collection);
        Assert.Equal("usuarios", collection.CollectionNamespace.CollectionName);
    }

    [Fact]
    public void CountersCollection_ShouldReturnCorrectCollection()
    {
        // Arrange - Instancia contexto com configuração mockada
        // Act - Acessa a coleção de contadores
        var context = new MongoDbContext(_configMock.Object);
        var collection = context.Counters;

        // Assert - Verifica se a coleção correta foi retornada
        Assert.NotNull(collection);
        Assert.Equal("contadores", collection.CollectionNamespace.CollectionName);
    }

    [Fact]
    public void PatientsCollection_ShouldReturnCorrectCollection()
    {
        // Arrange - Instancia contexto com configuração mockada
        // Act - Acessa a coleção de pacientes
        var context = new MongoDbContext(_configMock.Object);
        var collection = context.Patients;

        // Assert - Verifica se a coleção correta foi retornada
        Assert.NotNull(collection);
        Assert.Equal("pacientes", collection.CollectionNamespace.CollectionName);
    }

    [Fact]
    public void EmotionMappingsCollection_ShouldReturnCorrectCollection()
    {
        // Arrange - Instancia contexto com configuração mockada
        // Act - Acessa a coleção de mapeamentos de emoções
        var context = new MongoDbContext(_configMock.Object);
        var collection = context.EmotionMappings;

        // Assert - Verifica se a coleção correta foi retornada
        Assert.NotNull(collection);
        Assert.Equal("mapeamento_emocoes", collection.CollectionNamespace.CollectionName);
    }

    [Fact]
    public void HistoryRecordsCollection_ShouldReturnCorrectCollection()
    {
        // Arrange - Instancia contexto com configuração mockada
        // Act - Acessa a coleção de registros de histórico
        var context = new MongoDbContext(_configMock.Object);
        var collection = context.HistoryRecords;

        // Assert - Verifica se a coleção correta foi retornada
        Assert.NotNull(collection);
        Assert.Equal("historico", collection.CollectionNamespace.CollectionName);
    }

    [Fact]
    public void PasswordResetTokensCollection_ShouldReturnCorrectCollection()
    {
        // Arrange - Instancia contexto com configuração mockada
        // Act - Acessa a coleção de tokens de reset de senha
        var context = new MongoDbContext(_configMock.Object);
        var collection = context.PasswordResetTokens;

        // Assert - Verifica se a coleção correta foi retornada
        Assert.NotNull(collection);
        Assert.Equal("tokens_reset_senha", collection.CollectionNamespace.CollectionName);
    }

    #endregion
}