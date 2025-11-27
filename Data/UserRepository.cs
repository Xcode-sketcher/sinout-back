using MongoDB.Driver;
using APISinout.Models;
using Microsoft.Extensions.Logging;

namespace APISinout.Data;

// Interface para operações de repositório de usuários.
public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task CreateUserAsync(User user);
    Task UpdateUserAsync(string id, User user);
    Task DeleteUserAsync(string id);
    Task<List<User>> GetAllAsync();
}

// Implementação do repositório de usuários usando MongoDB.
public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly ILogger<UserRepository>? _logger;

    // Construtor que injeta o contexto do MongoDB.
    public UserRepository(MongoDbContext context, ILogger<UserRepository>? logger = null)
    {
        _users = context.Users;
        _logger = logger;
    }

    // Construtor para testes - permite injeção direta das coleções.
    public UserRepository(IMongoCollection<User> usersCollection, ILogger<UserRepository>? logger = null)
    {
        _users = usersCollection;
        _logger = logger;
    }

    // Obtém usuário por ID.
    public async Task<User?> GetByIdAsync(string userId)
    {
        return await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }

    // Obtém usuário por email.
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    // Cria um novo usuário.
    public async Task CreateUserAsync(User user)
    {
        _logger?.LogDebug("Creating user (no sensitive info). Email={Email}", user.Email);
        await _users.InsertOneAsync(user);
        _logger?.LogInformation("User created: Id={UserId}", user.Id);
    }


    // Atualiza um usuário existente.
    public async Task UpdateUserAsync(string userId, User user)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update
            .Set(u => u.Name, user.Name)
            .Set(u => u.Email, user.Email)
            .Set(u => u.Role, user.Role)
            .Set(u => u.Phone, user.Phone)
            .Set(u => u.PasswordHash, user.PasswordHash)
            .Set(u => u.LastLogin, user.LastLogin)
            .Set(u => u.UpdatedAt, user.UpdatedAt);
        await _users.UpdateOneAsync(filter, update);
        _logger?.LogInformation("User updated: Id={UserId}", userId);
    }

    // Remove um usuário.
    public async Task DeleteUserAsync(string userId)
    {
        await _users.DeleteOneAsync(u => u.Id == userId);
        _logger?.LogInformation("User deleted: Id={UserId}", userId);
    }

    // Lista todos os usuários.
    public async Task<List<User>> GetAllAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }
}

// Classe para contadores de sequências no MongoDB.
public class Counter
{
    // Identificador do contador.
    public string? Id { get; set; }
    // Sequência atual.
    public int Seq { get; set; }
}