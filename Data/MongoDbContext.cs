// --- CONTEXTO DO MONGODB: O MAPA DO JOGO ---
// Analogia de jogo: O MongoDbContext é como o "mapa principal" do jogo!
// Ele define onde estão todas as "regiões" (coleções) e como se conectar ao mundo (banco de dados).
// Sem o mapa, os jogadores se perdem!

using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using APISinout.Models;

namespace APISinout.Data;

public class MongoDbContext
{
    // A "conexão mágica" com o mundo do jogo
    private readonly IMongoDatabase _database;

    // Construtor: Como abrir o mapa e se conectar ao servidor
    public MongoDbContext(IConfiguration config)
    {
        var client = new MongoClient(config["MongoDb:ConnectionString"]);
        _database = client.GetDatabase(config["MongoDb:DatabaseName"]);
        
        // Configurar mapeamentos para compatibilidade
        ConfigureMappings();
    }
    
    private void ConfigureMappings()
    {
        // Configurar mapeamento personalizado para User
        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.MapIdProperty(u => u.Id); // _id como ObjectId
                cm.MapProperty(u => u.UserId).SetElementName("id_usuario"); // ID numérico sequencial
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
                
                // Permitir campos extras sem erro
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    // As "regiões" do mapa: onde ficam os dados do sistema
    public IMongoCollection<User> Users => _database.GetCollection<User>("usuarios");
    public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("contadores");
    public IMongoCollection<Patient> Patients => _database.GetCollection<Patient>("pacientes");
    public IMongoCollection<EmotionMapping> EmotionMappings => _database.GetCollection<EmotionMapping>("mapeamento_emocoes");
    public IMongoCollection<HistoryRecord> HistoryRecords => _database.GetCollection<HistoryRecord>("historico");
    public IMongoCollection<PasswordResetToken> PasswordResetTokens => _database.GetCollection<PasswordResetToken>("tokens_reset_senha");
}