// --- REPOSITÓRIO DE HISTÓRICO ---
// Gerencia o histórico de análises faciais e emoções detectadas

using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

public interface IHistoryRepository
{
    Task<HistoryRecord?> GetByIdAsync(string id);
    Task<List<HistoryRecord>> GetByUserIdAsync(int userId, int hours = 24);
    Task<List<HistoryRecord>> GetByFilterAsync(HistoryFilter filter);
    Task CreateRecordAsync(HistoryRecord record);
    Task DeleteOldRecordsAsync(int hours = 24);
    Task<PatientStatistics> GetUserStatisticsAsync(int userId, int hours = 24);
}

public class HistoryRepository : IHistoryRepository
{
    private readonly IMongoCollection<HistoryRecord> _history;

    public HistoryRepository(MongoDbContext context)
    {
        _history = context.HistoryRecords;
    }

    public async Task<HistoryRecord?> GetByIdAsync(string id)
    {
        return await _history.Find(h => h.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<HistoryRecord>> GetByUserIdAsync(int userId, int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        return await _history.Find(h => h.UserId == userId && h.Timestamp >= cutoffTime)
            .SortByDescending(h => h.Timestamp)
            .ToListAsync();
    }

    public async Task<List<HistoryRecord>> GetByFilterAsync(HistoryFilter filter)
    {
        var builder = Builders<HistoryRecord>.Filter;
        var filters = new List<FilterDefinition<HistoryRecord>>();

        if (filter.PatientId.HasValue)
            filters.Add(builder.Eq(h => h.UserId, filter.PatientId.Value));

        if (filter.StartDate.HasValue)
            filters.Add(builder.Gte(h => h.Timestamp, filter.StartDate.Value));

        if (filter.EndDate.HasValue)
            filters.Add(builder.Lte(h => h.Timestamp, filter.EndDate.Value));

        if (!string.IsNullOrEmpty(filter.DominantEmotion))
            filters.Add(builder.Eq(h => h.DominantEmotion, filter.DominantEmotion));

        if (filter.HasMessage.HasValue)
        {
            if (filter.HasMessage.Value)
                filters.Add(builder.Ne(h => h.MessageTriggered, null));
            else
                filters.Add(builder.Eq(h => h.MessageTriggered, null));
        }

        var combinedFilter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

        return await _history.Find(combinedFilter)
            .SortByDescending(h => h.Timestamp)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Limit(filter.PageSize)
            .ToListAsync();
    }

    public async Task CreateRecordAsync(HistoryRecord record)
    {
        Console.WriteLine($"[DEBUG REPO] Inserindo no MongoDB - UserId: {record.UserId}, Emotion: {record.DominantEmotion}");
        try
        {
            await _history.InsertOneAsync(record);
            Console.WriteLine($"[DEBUG REPO] ✅ Inserção bem-sucedida! ID: {record.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG REPO] ❌ ERRO ao inserir: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteOldRecordsAsync(int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        await _history.DeleteManyAsync(h => h.Timestamp < cutoffTime);
    }

    public async Task<PatientStatistics> GetUserStatisticsAsync(int userId, int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        var records = await _history.Find(h => h.UserId == userId && h.Timestamp >= cutoffTime)
            .ToListAsync();

        var stats = new PatientStatistics
        {
            PatientId = userId,
            StartPeriod = cutoffTime,
            EndPeriod = DateTime.UtcNow,
            TotalAnalyses = records.Count,
            EmotionFrequency = new Dictionary<string, int>(),
            MessageFrequency = new Dictionary<string, int>(),
            EmotionTrends = new List<EmotionTrend>()
        };

        // Calcular frequência de emoções
        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.DominantEmotion))
            {
                if (!stats.EmotionFrequency.ContainsKey(record.DominantEmotion))
                    stats.EmotionFrequency[record.DominantEmotion] = 0;
                stats.EmotionFrequency[record.DominantEmotion]++;
            }

            // Calcular frequência de mensagens
            if (!string.IsNullOrEmpty(record.MessageTriggered))
            {
                if (!stats.MessageFrequency.ContainsKey(record.MessageTriggered))
                    stats.MessageFrequency[record.MessageTriggered] = 0;
                stats.MessageFrequency[record.MessageTriggered]++;
            }
        }

        // Emoção mais frequente
        if (stats.EmotionFrequency.Count > 0)
            stats.MostFrequentEmotion = stats.EmotionFrequency.OrderByDescending(x => x.Value).First().Key;

        // Mensagem mais frequente
        if (stats.MessageFrequency.Count > 0)
            stats.MostFrequentMessage = stats.MessageFrequency.OrderByDescending(x => x.Value).First().Key;

        // Tendências por hora
        var groupedByHour = records
            .GroupBy(r => r.Timestamp.ToString("yyyy-MM-dd HH:00"))
            .Select(g => new EmotionTrend
            {
                Hour = g.Key,
                AnalysisCount = g.Count(),
                AverageEmotions = CalculateAverageEmotions(g.ToList())
            })
            .OrderBy(t => t.Hour)
            .ToList();

        stats.EmotionTrends = groupedByHour;

        return stats;
    }

    private Dictionary<string, double> CalculateAverageEmotions(List<HistoryRecord> records)
    {
        var emotionSums = new Dictionary<string, List<double>>();

        foreach (var record in records)
        {
            if (record.EmotionsDetected != null)
            {
                foreach (var emotion in record.EmotionsDetected)
                {
                    if (!emotionSums.ContainsKey(emotion.Key))
                        emotionSums[emotion.Key] = new List<double>();
                    emotionSums[emotion.Key].Add(emotion.Value);
                }
            }
        }

        return emotionSums.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Average()
        );
    }
}
