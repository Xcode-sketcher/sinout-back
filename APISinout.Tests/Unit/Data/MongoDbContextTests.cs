// Tests for MongoDbContext initialization and collection access.

using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using APISinout.Data;
using APISinout.Models;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;

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
    public void ConfigureMappings_ShouldRegisterUserClassMap_WhenNotRegistered()
    {
        // Arrange - Limpa o registro usando reflexão se existir
        var classMapType = typeof(BsonClassMap);
        var registeredClassMapsField = classMapType.GetField("_registeredClassMaps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (registeredClassMapsField != null)
        {
            var registeredClassMaps = (System.Collections.IDictionary)registeredClassMapsField.GetValue(null);
            if (registeredClassMaps != null && registeredClassMaps.Contains(typeof(User)))
            {
                registeredClassMaps.Remove(typeof(User));
            }
        }

        // Act - Chama ConfigureMappings diretamente usando reflexão
        var contextType = typeof(MongoDbContext);
        var configureMappingsMethod = contextType.GetMethod("ConfigureMappings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (configureMappingsMethod != null)
        {
            // Create context instance without calling constructor
            var contextInstance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);
            configureMappingsMethod.Invoke(contextInstance, null);
        }

        // Assert - Verifica que o mapeamento foi registrado
        bool isRegisteredAfter = BsonClassMap.IsClassMapRegistered(typeof(User));
        Assert.True(isRegisteredAfter, "User class map should be registered after ConfigureMappings");

        // Verify we can lookup the class map
        try
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(User));
            Assert.NotNull(classMap);
        }
        catch
        {
            Assert.Fail("Should be able to lookup the registered class map");
        }
    }

    [Fact]
    public void ConfigureMappings_ShouldNotReRegisterUserClassMap_WhenAlreadyRegistered()
    {
        // Arrange - Configura o cenário com o mapeamento já registrado
        var contextType = typeof(MongoDbContext);
        var configureMappingsMethod = contextType.GetMethod("ConfigureMappings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Ensure BsonClassMap is registered
        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.MapIdProperty(u => u.Id);
                cm.MapProperty(u => u.UserId).SetElementName("id_usuario");
                cm.MapProperty(u => u.Name).SetElementName("nome");
                cm.MapProperty(u => u.Email).SetElementName("email");
                cm.MapProperty(u => u.DataCadastro).SetElementName("data_cadastro");
                cm.MapProperty(u => u.Status).SetElementName("status");
                cm.MapProperty(u => u.Role).SetElementName("cargo");
                cm.MapProperty(u => u.PasswordHash).SetElementName("password_hash");
                cm.MapProperty(u => u.CreatedBy).SetElementName("criado_por");
                cm.MapProperty(u => u.LastLogin).SetElementName("ultimo_acesso");
                cm.MapProperty(u => u.Phone).SetElementName("telefone");
                cm.MapProperty(u => u.UpdatedAt).SetElementName("data_atualizacao");
                cm.MapProperty(u => u.PatientName).SetElementName("nome_paciente");
                cm.SetIgnoreExtraElements(true);
            });
        }

        // Verify it is registered before
        Assert.True(BsonClassMap.IsClassMapRegistered(typeof(User)), "User class map should be registered before calling ConfigureMappings");

        // Act - Chama ConfigureMappings quando já está registrado
        var contextInstance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);
        configureMappingsMethod.Invoke(contextInstance, null);

        // Assert - Verifica que ainda está registrado
        Assert.True(BsonClassMap.IsClassMapRegistered(typeof(User)), "User class map should still be registered after ConfigureMappings");
    }

    #endregion

    #region Collection Properties Tests

    [Fact]
    public void Collections_ShouldBeAccessible()
    {
        // Arrange - Configura o mock de configuração e limpa mapeamentos
        var classMapType = typeof(BsonClassMap);
        var registeredClassMapsField = classMapType.GetField("_registeredClassMaps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (registeredClassMapsField != null)
        {
            var registeredClassMaps = (System.Collections.IDictionary)registeredClassMapsField.GetValue(null);
            if (registeredClassMaps != null && registeredClassMaps.Contains(typeof(User)))
            {
                registeredClassMaps.Remove(typeof(User));
            }
        }

        var configMock = new Mock<IConfiguration>();
        // Use a mock connection string
        configMock.Setup(c => c["MongoDb:ConnectionString"]).Returns("mongodb://test.example.com:27017");
        configMock.Setup(c => c["MongoDb:DatabaseName"]).Returns("testdb");

        // Act & Assert - Tenta criar o contexto e acessar as coleções
        var context = new MongoDbContext(configMock.Object);
        
        // Assert - Verifica se as coleções não são nulas
        Assert.NotNull(context.Users);
        Assert.NotNull(context.Counters);
        Assert.NotNull(context.Patients);
        Assert.NotNull(context.EmotionMappings);
        Assert.NotNull(context.HistoryRecords);
        Assert.NotNull(context.PasswordResetTokens);
        
        // Verify mapping
        var isUserMapRegistered = BsonClassMap.IsClassMapRegistered(typeof(User));
        Assert.True(isUserMapRegistered, "User class map should be registered");
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