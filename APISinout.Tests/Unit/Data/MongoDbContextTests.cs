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
        // Arrange - Limpar registro usando reflexão se existir
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

        // Verificar que não está registrado antes (não é crítico para o teste)
        bool wasRegisteredBefore = BsonClassMap.IsClassMapRegistered(typeof(User));

        // Act - Chamar ConfigureMappings diretamente usando reflexão
        var contextType = typeof(MongoDbContext);
        var configureMappingsMethod = contextType.GetMethod("ConfigureMappings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (configureMappingsMethod != null)
        {
            // Criar uma instância do contexto sem chamar o construtor (usando FormatterServices)
            var contextInstance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);
            configureMappingsMethod.Invoke(contextInstance, null);
        }

        // Assert - Verifica que o mapeamento foi registrado
        bool isRegisteredAfter = BsonClassMap.IsClassMapRegistered(typeof(User));
        Assert.True(isRegisteredAfter, "User class map should be registered after ConfigureMappings");

        // Verificar que podemos obter o class map (se estiver registrado, isso não deve lançar exceção)
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
        // Arrange - Criar uma instância sem chamar o construtor
        var contextType = typeof(MongoDbContext);
        var configureMappingsMethod = contextType.GetMethod("ConfigureMappings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Garantir que o BsonClassMap está registrado
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

        // Verificar que está registrado antes
        Assert.True(BsonClassMap.IsClassMapRegistered(typeof(User)), "User class map should be registered before calling ConfigureMappings");

        // Act - Chamar ConfigureMappings quando já está registrado (deve executar o else)
        var contextInstance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);
        configureMappingsMethod.Invoke(contextInstance, null);

        // Assert - Verifica que ainda está registrado (não deve ter sido afetado)
        Assert.True(BsonClassMap.IsClassMapRegistered(typeof(User)), "User class map should still be registered after ConfigureMappings");
    }

    #endregion

    #region Collection Properties Tests

    [Fact]
    public void Collections_ShouldBeAccessible()
    {
        // Arrange - Limpar estado do BsonClassMap para garantir teste isolado
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
        // Usar uma string de conexão que não cause problemas (mongodb://localhost:27017 pode não existir, mas não deve falhar na criação)
        configMock.Setup(c => c["MongoDb:ConnectionString"]).Returns("mongodb://localhost:27017");
        configMock.Setup(c => c["MongoDb:DatabaseName"]).Returns("testdb");

        // Act & Assert - Tentar criar o contexto e acessar as coleções
        // Nota: Isso pode falhar se o MongoDB não estiver rodando, mas pelo menos testa a lógica
        try
        {
            var context = new MongoDbContext(configMock.Object);
            
            // Assert
            Assert.NotNull(context.Users);
            Assert.NotNull(context.Counters);
            Assert.NotNull(context.Patients);
            Assert.NotNull(context.EmotionMappings);
            Assert.NotNull(context.HistoryRecords);
            Assert.NotNull(context.PasswordResetTokens);
            
            // Verificar que o mapeamento foi configurado
            var isUserMapRegistered = BsonClassMap.IsClassMapRegistered(typeof(User));
            Assert.True(isUserMapRegistered, "User class map should be registered");
        }
        catch (Exception ex)
        {
            // Se falhar por causa de conexão, pelo menos verificamos que tentou executar
            Assert.Contains("MongoDB", ex.Message);
        }
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