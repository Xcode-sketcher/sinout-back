using APISinout.Models;
using MongoDB.Bson;

namespace APISinout.Tests.Fixtures;

public static class HistoryFixtures
{
    public static HistoryRecord CreateValidHistoryRecord(string? id = null, string? userId = null, string? patientId = null)
    {
        var uid = userId ?? ObjectId.GenerateNewId().ToString();
        var pid = patientId ?? ObjectId.GenerateNewId().ToString();
        return new HistoryRecord
        {
            Id = id ?? ObjectId.GenerateNewId().ToString(),
            UserId = uid,
            PatientId = pid,
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

    public static HistoryRecordResponse CreateValidHistoryRecordResponse(string? id = null, string? userId = null, string? patientId = null)
    {
        return new HistoryRecordResponse(CreateValidHistoryRecord(id, userId, patientId));
    }

    public static PatientStatistics CreateValidPatientStatistics(string? patientId = null)
    {
        return new PatientStatistics
        {
            PatientId = patientId ?? ObjectId.GenerateNewId().ToString(),
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
            PatientId = ObjectId.GenerateNewId().ToString(),
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            DominantEmotion = "happy",
            HasMessage = true,
            PageNumber = 1,
            PageSize = 50
        };
    }

    public static APISinout.Controllers.CuidadorEmotionRequest CreateValidCuidadorEmotionRequest(string? cuidadorId = null)
    {
        return new APISinout.Controllers.CuidadorEmotionRequest
        {
            cuidadorId = cuidadorId ?? ObjectId.GenerateNewId().ToString(),
            patientId = ObjectId.GenerateNewId().ToString(),
            patientName = "João Silva",
            emotionsDetected = new Dictionary<string, double>
            {
                { "happy", 0.8 },
                { "sad", 0.1 },
                { "neutral", 0.1 }
            },
            dominantEmotion = "happy",
            age = "45",
            gender = "M",
            timestamp = DateTime.UtcNow
        };
    }
}