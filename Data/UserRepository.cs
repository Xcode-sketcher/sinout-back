using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

public class Counter
{
    public string? Id { get; set; }
    public int Seq { get; set; }
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task CreateUserAsync(User user);
    Task UpdateUserAsync(int id, User user);
    Task DeleteUserAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<int> GetNextUserIdAsync();
}

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Counter> _counters;

    public UserRepository(MongoDbContext context)
    {
        _users = context.Users;
        _counters = context.Counters;
    }

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

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task CreateUserAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }

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

    public async Task DeleteUserAsync(int id)
    {
        await _users.DeleteOneAsync(u => u.Id == id);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }
}