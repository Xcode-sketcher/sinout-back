// --- REPOSITÓRIO DE MAPEAMENTO DE EMOÇÕES ---
// Gerencia as regras de mapeamento entre emoções e mensagens

using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

public interface IEmotionMappingRepository
{
    Task<EmotionMapping?> GetByIdAsync(string id);
    Task<List<EmotionMapping>> GetByUserIdAsync(int userId);
    Task<List<EmotionMapping>> GetActiveByUserIdAsync(int userId);
    Task<List<EmotionMapping>> GetByUserAndEmotionAsync(int userId, string emotion);
    Task CreateMappingAsync(EmotionMapping mapping);
    Task UpdateMappingAsync(string id, EmotionMapping mapping);
    Task DeleteMappingAsync(string id);
    Task<int> CountByUserAndEmotionAsync(int userId, string emotion);
    Task<bool> ExistsAsync(string id);
}

public class EmotionMappingRepository : IEmotionMappingRepository
{
    private readonly IMongoCollection<EmotionMapping> _mappings;

    public EmotionMappingRepository(MongoDbContext context)
    {
        _mappings = context.EmotionMappings;
    }

    public async Task<EmotionMapping?> GetByIdAsync(string id)
    {
        return await _mappings.Find(m => m.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<EmotionMapping>> GetByUserIdAsync(int userId)
    {
        return await _mappings.Find(m => m.UserId == userId)
            .SortBy(m => m.Emotion)
            .ThenBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<List<EmotionMapping>> GetActiveByUserIdAsync(int userId)
    {
        return await _mappings.Find(m => m.UserId == userId && m.Active)
            .SortBy(m => m.Emotion)
            .ThenBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<List<EmotionMapping>> GetByUserAndEmotionAsync(int userId, string emotion)
    {
        return await _mappings.Find(m => m.UserId == userId && m.Emotion == emotion && m.Active)
            .SortBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task CreateMappingAsync(EmotionMapping mapping)
    {
        await _mappings.InsertOneAsync(mapping);
    }

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
    }

    public async Task DeleteMappingAsync(string id)
    {
        // Soft delete: marcar como inativo
        var filter = Builders<EmotionMapping>.Filter.Eq(m => m.Id, id);
        var update = Builders<EmotionMapping>.Update
            .Set(m => m.Active, false)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);
        
        await _mappings.UpdateOneAsync(filter, update);
    }

    public async Task<int> CountByUserAndEmotionAsync(int userId, string emotion)
    {
        var count = await _mappings.CountDocumentsAsync(m => 
            m.UserId == userId && 
            m.Emotion == emotion && 
            m.Active);
        
        return (int)count;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        var count = await _mappings.CountDocumentsAsync(m => m.Id == id);
        return count > 0;
    }
}
