using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using APISinout.Models;

namespace APISinout.Data;

// Contexto para acesso ao MongoDB.
// Gerencia a conexão com o banco de dados e configurações de mapeamento.
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    // Construtor que inicializa a conexão com o MongoDB.
    public MongoDbContext(IConfiguration config)
    {
        var client = new MongoClient(config["MongoDb:ConnectionString"]);
        _database = client.GetDatabase(config["MongoDb:DatabaseName"]);

        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        // Configurar mapeamento personalizado para User
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
    }

    // Coleções do banco de dados.
    public IMongoCollection<User> Users => _database.GetCollection<User>("usuarios");
    public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("contadores");
    public IMongoCollection<Patient> Patients => _database.GetCollection<Patient>("pacientes");
    public IMongoCollection<EmotionMapping> EmotionMappings => _database.GetCollection<EmotionMapping>("mapeamento_emocoes");
    public IMongoCollection<HistoryRecord> HistoryRecords => _database.GetCollection<HistoryRecord>("historico");
    public IMongoCollection<PasswordResetToken> PasswordResetTokens => _database.GetCollection<PasswordResetToken>("tokens_reset_senha");
}