// --- MODELO DE HISTÓRICO: O DIÁRIO DE EMOÇÕES ---
// Este modelo armazena o histórico de análises faciais por paciente.
// Mantém registro das emoções detectadas e palavras disparadas nas últimas 24 horas,
// permitindo ao cuidador acompanhar padrões e estatísticas.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

public class HistoryRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("id_usuario")]
    public int UserId { get; set; } // ID do cuidador
    
    [BsonElement("nome_paciente")]
    public string? PatientName { get; set; } // Nome do paciente
    
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } // Quando a análise ocorreu
    
    [BsonElement("emocoes_detectadas")]
    public Dictionary<string, double>? EmotionsDetected { get; set; } // Todas as emoções com %
    
    [BsonElement("emocao_dominante")]
    public string? DominantEmotion { get; set; } // A emoção principal
    
    [BsonElement("percentual_dominante")]
    public double DominantPercentage { get; set; }
    
    [BsonElement("mensagem_disparada")]
    public string? MessageTriggered { get; set; } // Palavra exibida (se houver)
    
    [BsonElement("regra_acionada_id")]
    public string? TriggeredRuleId { get; set; } // ID do EmotionMapping que foi acionado
}

// Modelo para criar registro de histórico
public class HistoryRecordRequest
{
    public int PatientId { get; set; }
    public Dictionary<string, double>? EmotionsDetected { get; set; }
    public string? DominantEmotion { get; set; }
    public double DominantPercentage { get; set; }
    public string? MessageTriggered { get; set; }
    public string? TriggeredRuleId { get; set; }
}

// Modelo de resposta do histórico
public class HistoryRecordResponse
{
    public string? Id { get; set; }
    public int PatientId { get; set; }
    public string? PatientName { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, double>? EmotionsDetected { get; set; }
    public string? DominantEmotion { get; set; }
    public double DominantPercentage { get; set; }
    public string? MessageTriggered { get; set; }

    public HistoryRecordResponse(HistoryRecord record, string? userName = null)
    {
        Id = record.Id;
        PatientId = record.UserId;
        PatientName = record.PatientName ?? userName;
        Timestamp = record.Timestamp;
        EmotionsDetected = record.EmotionsDetected;
        DominantEmotion = record.DominantEmotion;
        DominantPercentage = record.DominantPercentage;
        MessageTriggered = record.MessageTriggered;
    }
}

// Modelo para estatísticas do dashboard
public class PatientStatistics
{
    public int PatientId { get; set; }
    public string? PatientName { get; set; }
    public DateTime StartPeriod { get; set; } // Início do período (últimas 24h)
    public DateTime EndPeriod { get; set; } // Fim do período (agora)
    public int TotalAnalyses { get; set; } // Total de análises no período
    public Dictionary<string, int>? EmotionFrequency { get; set; } // Frequência de cada emoção
    public Dictionary<string, int>? MessageFrequency { get; set; } // Frequência de cada palavra
    public string? MostFrequentEmotion { get; set; } // Emoção mais comum
    public string? MostFrequentMessage { get; set; } // Mensagem mais disparada
    public List<EmotionTrend>? EmotionTrends { get; set; } // Tendências horárias
}

// Tendência de emoção por período
public class EmotionTrend
{
    public string? Hour { get; set; } // Hora do dia (ex: "14:00")
    public Dictionary<string, double>? AverageEmotions { get; set; } // Média % de cada emoção
    public int AnalysisCount { get; set; } // Quantas análises nesta hora
}

// Filtros para consulta de histórico
public class HistoryFilter
{
    public int? PatientId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DominantEmotion { get; set; }
    public bool? HasMessage { get; set; } // Se disparou mensagem
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
