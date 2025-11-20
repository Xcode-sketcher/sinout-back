using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

// Interface para operações de repositório de usuários.
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task CreateUserAsync(User user);
    Task UpdateUserAsync(int id, User user);
    Task DeleteUserAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<int> GetNextUserIdAsync();
    Task UpdatePatientNameAsync(int userId, string patientName);
}

// Implementação do repositório de usuários usando MongoDB.
public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Counter> _counters;

    // Construtor que injeta o contexto do MongoDB.
    public UserRepository(MongoDbContext context)
    {
        _users = context.Users;
        _counters = context.Counters;
    }

    // Obtém usuário por ID numérico.
    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();
    }

    // Obtém usuário por email.
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    // Cria um novo usuário.
    public async Task CreateUserAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }

    // Atualiza um usuário existente.
    public async Task UpdateUserAsync(int userId, User user)
    {
        var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
        var update = Builders<User>.Update
            .Set(u => u.Name, user.Name)
            .Set(u => u.Email, user.Email)
            .Set(u => u.Status, user.Status)
            .Set(u => u.Role, user.Role)
            .Set(u => u.Phone, user.Phone)
            .Set(u => u.PasswordHash, user.PasswordHash)
            .Set(u => u.LastLogin, user.LastLogin)
            .Set(u => u.UpdatedAt, user.UpdatedAt);
        await _users.UpdateOneAsync(filter, update);
    }

    // Remove um usuário.
    public async Task DeleteUserAsync(int userId)
    {
        await _users.DeleteOneAsync(u => u.UserId == userId);
    }

    // Lista todos os usuários.
    public async Task<List<User>> GetAllAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }

    // Obtém o próximo ID disponível para usuário.
    public async Task<int> GetNextUserIdAsync()
    {
        var filter = Builders<Counter>.Filter.Eq(c => c.Id, "user");
        var update = Builders<Counter>.Update.Inc(c => c.Seq, 1);
        var options = new FindOneAndUpdateOptions<Counter> {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = true
        };

        var counter = await _counters.FindOneAndUpdateAsync(filter, update, options);
        return counter?.Seq ?? 1;
    }

    // Atualiza o nome do paciente associado ao usuário.
    public async Task UpdatePatientNameAsync(int userId, string patientName)
    {
        Console.WriteLine($"[UserRepository] Atualizando patient_name no MongoDB - UserId={userId}");

        var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
        var update = Builders<User>.Update
            .Set(u => u.PatientName, patientName)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await _users.UpdateOneAsync(filter, update);

        Console.WriteLine($"[UserRepository] patient_name atualizado: '{patientName}'");
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