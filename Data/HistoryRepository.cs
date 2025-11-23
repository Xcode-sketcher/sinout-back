using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

// Interface para operações de repositório de histórico.
public interface IHistoryRepository
{
    Task<HistoryRecord?> GetByIdAsync(string id);
    Task<List<HistoryRecord>> GetByPatientIdAsync(string patientId, int hours = 24);
    Task<List<HistoryRecord>> GetByFilterAsync(HistoryFilter filter);
    Task CreateRecordAsync(HistoryRecord record);
    Task DeleteOldRecordsAsync(int hours = 24);
    Task<PatientStatistics> GetPatientStatisticsAsync(string patientId, int hours = 24);
}

// Implementação do repositório de histórico usando MongoDB.
public class HistoryRepository : IHistoryRepository
{
    private readonly IMongoCollection<HistoryRecord> _history;

    // Construtor que injeta o contexto do MongoDB.
    public HistoryRepository(MongoDbContext context)
    {
        _history = context.HistoryRecords;
    }

    // Construtor para testes unitários.
    public HistoryRepository(IMongoCollection<HistoryRecord> historyCollection)
    {
        _history = historyCollection;
    }

    // Obtém registro de histórico por ID.
    public async Task<HistoryRecord?> GetByIdAsync(string id)
    {
        return await _history.Find(h => h.Id == id).FirstOrDefaultAsync();
    }

    // Obtém registros de histórico por ID do paciente.
    public async Task<List<HistoryRecord>> GetByPatientIdAsync(string patientId, int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        return await _history.Find(h => h.PatientId == patientId && h.Timestamp >= cutoffTime)
            .SortByDescending(h => h.Timestamp)
            .ToListAsync();
    }

    // Obtém registros de histórico por filtro.
    public async Task<List<HistoryRecord>> GetByFilterAsync(HistoryFilter filter)
    {
        var builder = Builders<HistoryRecord>.Filter;
        var filters = new List<FilterDefinition<HistoryRecord>>();

        if (!string.IsNullOrEmpty(filter.PatientId))
            filters.Add(builder.Eq(h => h.PatientId, filter.PatientId));

        if (filter.CuidadorId != null)
            filters.Add(builder.Eq(h => h.UserId, filter.CuidadorId));

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

    // Cria um novo registro de histórico.
    public async Task CreateRecordAsync(HistoryRecord record)
    {
        Console.WriteLine("💾 HistoryRepository.CreateRecordAsync");
        Console.WriteLine($"   EmotionsDetected: {record.EmotionsDetected?.Count ?? 0} emotions");
        if (record.EmotionsDetected != null)
        {
            foreach (var kvp in record.EmotionsDetected)
                Console.WriteLine($"      {kvp.Key}: {kvp.Value}");
        }
        
        await _history.InsertOneAsync(record);
        Console.WriteLine("✅ Registro inserido no MongoDB com sucesso!");
    }

    // Remove registros antigos de histórico.
    public async Task DeleteOldRecordsAsync(int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        await _history.DeleteManyAsync(h => h.Timestamp < cutoffTime);
    }

    // Obtém estatísticas do paciente.
    public async Task<PatientStatistics> GetPatientStatisticsAsync(string patientId, int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        var records = await _history.Find(h => h.PatientId == patientId && h.Timestamp >= cutoffTime)
            .ToListAsync();

        var stats = new PatientStatistics
        {
            PatientId = patientId,
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

    // Calcula médias de emoções.
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
