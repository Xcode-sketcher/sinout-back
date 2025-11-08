// --- REPOSITÓRIO DE USUÁRIOS: O INVENTÁRIO DO JOGO ---
// Analogia de jogo: O UserRepository é como o "inventário" do jogo!
// Aqui guardamos todos os itens (usuários), organizamos, buscamos e gerenciamos.
// É onde os jogadores armazenam suas poções, armas e equipamentos.

using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

// A "interface" do inventário: lista o que podemos fazer
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id); // Pegar item específico
    Task<User?> GetByEmailAsync(string email); // Procurar por nome
    Task CreateUserAsync(User user); // Adicionar novo item
    Task UpdateUserAsync(int id, User user); // Modificar item
    Task DeleteUserAsync(int id); // Remover item
    Task<List<User>> GetAllAsync(); // Ver todo o inventário
    Task<int> GetNextUserIdAsync(); // Pegar próximo slot vazio
}

// A "implementação" do inventário
public class UserRepository : IUserRepository
{
    // Os "baús" onde guardamos os itens
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Counter> _counters;

    // Construtor: Abrir os baús com a chave (contexto)
    public UserRepository(MongoDbContext context)
    {
        _users = context.Users; // Baú dos usuários
        _counters = context.Counters; // Baú dos contadores
    }

    // Implementação: Pegar item por ID
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    // Implementação: Procurar por email
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    // Implementação: Adicionar novo item ao inventário
    public async Task CreateUserAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }

    // Implementação: Atualizar item existente
    public async Task UpdateUserAsync(int id, User user)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var update = Builders<User>.Update
            .Set(u => u.Name, user.Name)
            .Set(u => u.Email, user.Email)
            .Set(u => u.Status, user.Status)
            .Set(u => u.Role, user.Role);
        await _users.UpdateOneAsync(filter, update);
    }

    // Implementação: Remover item do inventário
    public async Task DeleteUserAsync(int id)
    {
        await _users.DeleteOneAsync(u => u.Id == id);
    }

    // Implementação: Listar todos os itens
    public async Task<List<User>> GetAllAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }

    // Implementação: Pegar próximo ID disponível (como encontrar slot vazio)
    public async Task<int> GetNextUserIdAsync()
    {
        var filter = Builders<Counter>.Filter.Eq(c => c.Id, "user");
        var update = Builders<Counter>.Update.Inc(c => c.Seq, 1);
        var options = new FindOneAndUpdateOptions<Counter> { 
            ReturnDocument = ReturnDocument.After,
            IsUpsert = true
        };
        
        var counter = await _counters.FindOneAndUpdateAsync(filter, update, options);
        return counter?.Seq ?? 1; // Se não existir, começar do 1
    }
}

// O "contador" do inventário: como um sistema de numeração automática
public class Counter
{
    public string? Id { get; set; } // Identificador do contador (ex: "user")
    public int Seq { get; set; } // Sequência atual
}