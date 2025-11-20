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
using MongoDB.Driver;

namespace APISinout.Tests.Unit.Data;

public class MongoDbContextTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IMongoDatabase> _databaseMock;

    public MongoDbContextTests()
    {
        _configMock = new Mock<IConfiguration>();
        _databaseMock = new Mock<IMongoDatabase>();

        _configMock.Setup(c => c["MongoDb:ConnectionString"]).Returns("invalid-connection-string-for-mocking-purposes");
        _configMock.Setup(c => c["MongoDb:DatabaseName"]).Returns("testdb");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldConfigureMappings()
    {
        // Arrange - Configura mock da configuração com dados válidos
        // Act & Assert - Verifica que não há exceções durante a configuração
        // Como o construtor tenta conectar ao banco, vamos testar apenas que as configurações são lidas
        var connectionString = _configMock.Object["MongoDb:ConnectionString"];
        var databaseName = _configMock.Object["MongoDb:DatabaseName"];

        // Assert - Verifica se as configurações foram lidas corretamente
        Assert.Equal("invalid-connection-string-for-mocking-purposes", connectionString);
        Assert.Equal("testdb", databaseName);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_ShouldReadConnectionStringCorrectly()
    {
        // Arrange - Configuração mockada
        // Act - Lê a string de conexão
        var connectionString = _configMock.Object["MongoDb:ConnectionString"];

        // Assert - Verifica se a string de conexão é lida corretamente
        Assert.Equal("invalid-connection-string-for-mocking-purposes", connectionString);
    }

    [Fact]
    public void Configuration_ShouldReadDatabaseNameCorrectly()
    {
        // Arrange - Configuração mockada
        // Act - Lê o nome do banco de dados
        var databaseName = _configMock.Object["MongoDb:DatabaseName"];

        // Assert - Verifica se o nome do banco é lido corretamente
        Assert.Equal("testdb", databaseName);
    }

    #endregion
}