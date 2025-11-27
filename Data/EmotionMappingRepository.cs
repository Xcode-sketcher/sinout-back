using MongoDB.Driver;
using APISinout.Models;
using Microsoft.Extensions.Logging;

namespace APISinout.Data;

// Interface para operações de repositório de mapeamento de emoções.
public interface IEmotionMappingRepository
{
    Task<EmotionMapping?> GetByIdAsync(string id);
    Task<List<EmotionMapping>> GetByUserIdAsync(string userId);
    Task<List<EmotionMapping>> GetActiveByUserIdAsync(string userId);
    Task<List<EmotionMapping>> GetByUserAndEmotionAsync(string userId, string emotion);
    Task CreateMappingAsync(EmotionMapping mapping);
    Task UpdateMappingAsync(string id, EmotionMapping mapping);
    Task DeleteMappingAsync(string id);
    Task<int> CountByUserAndEmotionAsync(string userId, string emotion);
    Task<bool> ExistsAsync(string id);
}

// Implementação do repositório de mapeamento de emoções usando MongoDB.
public class EmotionMappingRepository : IEmotionMappingRepository
{
    private readonly IMongoCollection<EmotionMapping> _mappings;
    private readonly ILogger<EmotionMappingRepository>? _logger;

    // Construtor que injeta o contexto do MongoDB.
    public EmotionMappingRepository(MongoDbContext context, ILogger<EmotionMappingRepository>? logger = null)
    {
        _mappings = context.EmotionMappings;
        _logger = logger;
    }

    // Construtor para testes - permite injeção direta da coleção.
    public EmotionMappingRepository(IMongoCollection<EmotionMapping> mappingsCollection, ILogger<EmotionMappingRepository>? logger = null)
    {
        _mappings = mappingsCollection;
        _logger = logger;
    }

    // Obtém mapeamento por ID.
    public async Task<EmotionMapping?> GetByIdAsync(string id)
    {
        return await _mappings.Find(m => m.Id == id).FirstOrDefaultAsync();
    }

    // Obtém mapeamentos por ID do usuário.
    public async Task<List<EmotionMapping>> GetByUserIdAsync(string userId)
    {
        return await _mappings.Find(m => m.UserId == userId)
            .SortBy(m => m.Emotion)
            .ThenBy(m => m.Priority)
            .ToListAsync();
    }

    // Obtém mapeamentos ativos por ID do usuário.
    public async Task<List<EmotionMapping>> GetActiveByUserIdAsync(string userId)
    {
        return await _mappings.Find(m => m.UserId == userId && m.Active)
            .SortBy(m => m.Emotion)
            .ThenBy(m => m.Priority)
            .ToListAsync();
    }

    // Obtém mapeamentos por usuário e emoção.
    public async Task<List<EmotionMapping>> GetByUserAndEmotionAsync(string userId, string emotion)
    {
        return await _mappings.Find(m => m.UserId == userId && m.Emotion == emotion && m.Active)
            .SortBy(m => m.Priority)
            .ToListAsync();
    }

    // Cria um novo mapeamento.
    public async Task CreateMappingAsync(EmotionMapping mapping)
    {
        _logger?.LogDebug("Creating emotion mapping (no sensitive content). UserId={UserId}", mapping.UserId);
        await _mappings.InsertOneAsync(mapping);
        _logger?.LogInformation("Emotion mapping created: Id={MappingId}", mapping.Id);
    }

    // Atualiza um mapeamento existente.
    public async Task UpdateMappingAsync(string id, EmotionMapping mapping)
    {
        var filter = Builders<EmotionMapping>.Filter.Eq(m => m.Id, id);
        var update = Builders<EmotionMapping>.Update
            .Set(m => m.Emotion, mapping.Emotion)
            .Set(m => m.IntensityLevel, mapping.IntensityLevel)
            .Set(m => m.MinPercentage, mapping.MinPercentage)
            .Set(m => m.Message, mapping.Message)
            .Set(m => m.Priority, mapping.Priority)
            .Set(m => m.Active, mapping.Active)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        await _mappings.UpdateOneAsync(filter, update);
        _logger?.LogInformation("Emotion mapping updated: Id={MappingId}", id);
    }

    // Remove um mapeamento (soft delete).
    public async Task DeleteMappingAsync(string id)
    {
        // Soft delete: marcar como inativo
        var filter = Builders<EmotionMapping>.Filter.Eq(m => m.Id, id);
        var update = Builders<EmotionMapping>.Update
            .Set(m => m.Active, false)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        await _mappings.UpdateOneAsync(filter, update);
        _logger?.LogInformation("Emotion mapping soft-deleted: Id={MappingId}", id);
    }

    // Conta mapeamentos por usuário e emoção.
    public async Task<int> CountByUserAndEmotionAsync(string userId, string emotion)
    {
        var count = await _mappings.CountDocumentsAsync(m =>
            m.UserId == userId &&
            m.Emotion == emotion &&
            m.Active);

        return (int)count;
    }

    // Verifica se o mapeamento existe.
    public async Task<bool> ExistsAsync(string id)
    {
        var count = await _mappings.CountDocumentsAsync(m => m.Id == id);
        return count > 0;
    }
}
