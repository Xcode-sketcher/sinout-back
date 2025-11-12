// --- REPOSITÓRIO DE TOKENS DE RESET DE SENHA ---
// Gerencia tokens temporários para redefinição de senha

using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

public interface IPasswordResetRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(int userId);
    Task CreateTokenAsync(PasswordResetToken resetToken);
    Task MarkAsUsedAsync(string id);
    Task DeleteExpiredTokensAsync();
}

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly IMongoCollection<PasswordResetToken> _tokens;

    public PasswordResetRepository(MongoDbContext context)
    {
        _tokens = context.PasswordResetTokens;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        return await _tokens.Find(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    public async Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(int userId)
    {
        return await _tokens.Find(t => t.UserId == userId && !t.Used && t.ExpiresAt > DateTime.UtcNow)
            .SortByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task CreateTokenAsync(PasswordResetToken resetToken)
    {
        await _tokens.InsertOneAsync(resetToken);
    }

    public async Task MarkAsUsedAsync(string id)
    {
        var filter = Builders<PasswordResetToken>.Filter.Eq(t => t.Id, id);
        var update = Builders<PasswordResetToken>.Update
            .Set(t => t.Used, true)
            .Set(t => t.UsedAt, DateTime.UtcNow);
        
        await _tokens.UpdateOneAsync(filter, update);
    }

    public async Task DeleteExpiredTokensAsync()
    {
        await _tokens.DeleteManyAsync(t => t.ExpiresAt < DateTime.UtcNow || t.Used);
    }
}
