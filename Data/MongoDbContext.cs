using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using APISinout.Models;

namespace APISinout.Data;

// Contexto para acesso ao MongoDB.
// Gerencia a conexão com o banco de dados e configurações de mapeamento.
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbContext>? _logger;

    // Construtor que inicializa a conexão com o MongoDB.
    public MongoDbContext(IConfiguration config, ILogger<MongoDbContext>? logger = null)
    {
        _logger = logger;
        var connectionString = config["MongoDb:ConnectionString"] ?? config.GetConnectionString("MongoDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger?.LogWarning("MongoDbContext: Connection string is not configured.");
            throw new InvalidOperationException("MongoDB connection string is not configured.");
        }

        var databaseName = config["MongoDb:DatabaseName"];
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            _logger?.LogError("MongoDbContext: Database name is not configured (MongoDb:DatabaseName).");
            throw new InvalidOperationException("MongoDB database name (MongoDb:DatabaseName) is not configured.");
        }

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        _logger?.LogInformation("MongoDbContext connected to database.");

        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        // Configurar mapeamento personalizado para User
        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    // Coleções do banco de dados.
    public IMongoCollection<User> Users => _database.GetCollection<User>("usuarios");
    public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("contadores");
    public IMongoCollection<Patient> Patients => _database.GetCollection<Patient>("pacientes");
    public IMongoCollection<EmotionMapping> EmotionMappings => _database.GetCollection<EmotionMapping>("mapeamento_emocoes");
    public IMongoCollection<HistoryRecord> HistoryRecords => _database.GetCollection<HistoryRecord>("historico");
    public IMongoCollection<PasswordResetToken> PasswordResetTokens => _database.GetCollection<PasswordResetToken>("tokens_reset_senha");
}