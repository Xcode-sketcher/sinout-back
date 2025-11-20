using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

// Interface para operações de repositório de tokens de reset de senha.
public interface IPasswordResetRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(int userId);
    Task CreateTokenAsync(PasswordResetToken resetToken);
    Task MarkAsUsedAsync(string id);
    Task DeleteExpiredTokensAsync();
}

// Implementação do repositório de tokens de reset de senha usando MongoDB.
public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly IMongoCollection<PasswordResetToken> _tokens;

    // Construtor que injeta o contexto do MongoDB.
    public PasswordResetRepository(MongoDbContext context)
    {
        _tokens = context.PasswordResetTokens;
    }

    // Construtor para testes - permite injeção direta da coleção.
    public PasswordResetRepository(IMongoCollection<PasswordResetToken> tokensCollection)
    {
        _tokens = tokensCollection;
    }

    // Obtém token de reset por token.
    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        return await _tokens.Find(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    // Obtém token ativo por ID do usuário.
    public async Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(int userId)
    {
        return await _tokens.Find(t => t.UserId == userId && !t.Used && t.ExpiresAt > DateTime.UtcNow)
            .SortByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    // Cria um novo token de reset.
    public async Task CreateTokenAsync(PasswordResetToken resetToken)
    {
        await _tokens.InsertOneAsync(resetToken);
    }

    // Marca token como usado.
    public async Task MarkAsUsedAsync(string id)
    {
        var filter = Builders<PasswordResetToken>.Filter.Eq(t => t.Id, id);
        var update = Builders<PasswordResetToken>.Update
            .Set(t => t.Used, true)
            .Set(t => t.UsedAt, DateTime.UtcNow);

        await _tokens.UpdateOneAsync(filter, update);
    }

    // Remove tokens expirados.
    public async Task DeleteExpiredTokensAsync()
    {
        await _tokens.DeleteManyAsync(t => t.ExpiresAt < DateTime.UtcNow || t.Used);
    }
}
