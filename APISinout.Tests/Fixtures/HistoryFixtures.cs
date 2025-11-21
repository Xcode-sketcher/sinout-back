using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class HistoryFixtures
{
    public static HistoryRecord CreateValidHistoryRecord(string? id = null, int userId = 1)
    {
        return new HistoryRecord
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            PatientName = "João Silva",
            Timestamp = DateTime.UtcNow,
            EmotionsDetected = new Dictionary<string, double>
            {
                { "happy", 0.8 },
                { "sad", 0.1 },
                { "neutral", 0.1 }
            },
            DominantEmotion = "happy",
            DominantPercentage = 0.8,
            MessageTriggered = "Paciente está feliz!",
            TriggeredRuleId = "rule1"
        };
    }

    public static HistoryRecordResponse CreateValidHistoryRecordResponse(string? id = null, int userId = 1)
    {
        return new HistoryRecordResponse(CreateValidHistoryRecord(id, userId));
    }

    public static PatientStatistics CreateValidPatientStatistics(int userId = 1)
    {
        return new PatientStatistics
        {
            PatientId = userId,
            PatientName = "João Silva",
            StartPeriod = DateTime.UtcNow.AddHours(-24),
            EndPeriod = DateTime.UtcNow,
            TotalAnalyses = 10,
            EmotionFrequency = new Dictionary<string, int>
            {
                { "happy", 6 },
                { "sad", 3 },
                { "neutral", 1 }
            },
            MessageFrequency = new Dictionary<string, int>
            {
                { "Paciente está feliz!", 6 },
                { "Paciente está triste", 3 }
            },
            MostFrequentEmotion = "happy",
            MostFrequentMessage = "Paciente está feliz!",
            EmotionTrends = new List<EmotionTrend>
            {
                new EmotionTrend
                {
                    Hour = "14:00",
                    AverageEmotions = new Dictionary<string, double> { { "happy", 0.8 } },
                    AnalysisCount = 2
                }
            }
        };
    }

    public static HistoryFilter CreateValidHistoryFilter()
    {
        return new HistoryFilter
        {
            PatientId = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            DominantEmotion = "happy",
            HasMessage = true,
            PageNumber = 1,
            PageSize = 50
        };
    }

    public static APISinout.Controllers.CuidadorEmotionRequest CreateValidCuidadorEmotionRequest(int cuidadorId = 1)
    {
        return new APISinout.Controllers.CuidadorEmotionRequest
        {
            CuidadorId = cuidadorId,
            PatientName = "João Silva",
            EmotionsDetected = new Dictionary<string, double>
            {
                { "happy", 0.8 },
                { "sad", 0.1 },
                { "neutral", 0.1 }
            },
            DominantEmotion = "happy",
            Age = "45",
            Gender = "M",
            Timestamp = DateTime.UtcNow
        };
    }
}