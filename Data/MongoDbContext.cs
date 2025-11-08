// --- CONTEXTO DO MONGODB: O MAPA DO JOGO ---
// Analogia de jogo: O MongoDbContext é como o "mapa principal" do jogo!
// Ele define onde estão todas as "regiões" (coleções) e como se conectar ao mundo (banco de dados).
// Sem o mapa, os jogadores se perdem!

using MongoDB.Driver;
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
    }

    // As "regiões" do mapa: onde ficam os usuários e os contadores
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("counters");
}