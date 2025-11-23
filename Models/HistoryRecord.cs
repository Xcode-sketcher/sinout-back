using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace APISinout.Models;

// Representa um registro de histórico de análise facial.
// Armazena dados de emoções detectadas e mensagens disparadas para um paciente.
public class HistoryRecord
{
    // ID único do registro no MongoDB.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // ID do usuário cuidador.
    [BsonElement("id_usuario")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UserId { get; set; }

    // ID do paciente.
    [BsonElement("id_paciente")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PatientId { get; set; }

    // Timestamp da análise.
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    // Dicionário com todas as emoções detectadas e seus percentuais.
    [BsonElement("emocoes_detectadas")]
    public Dictionary<string, double>? EmotionsDetected { get; set; }

    // Emoção dominante detectada.
    [BsonElement("emocao_dominante")]
    public string? DominantEmotion { get; set; }

    // Percentual da emoção dominante.
    [BsonElement("percentual_dominante")]
    public double DominantPercentage { get; set; }

    // Mensagem disparada, se houver.
    [BsonElement("mensagem_disparada")]
    public string? MessageTriggered { get; set; }

    // ID da regra de mapeamento que foi acionada.
    [BsonElement("regra_acionada_id")]
    public string? TriggeredRuleId { get; set; }
}

// Representa uma solicitação para criar um registro de histórico.
public class HistoryRecordRequest
{
    // ID do paciente.
    public string? PatientId { get; set; }

    // Emoções detectadas com percentuais.
    public Dictionary<string, double>? EmotionsDetected { get; set; }

    // Emoção dominante.
    public string? DominantEmotion { get; set; }

    // Percentual da emoção dominante.
    public double DominantPercentage { get; set; }

    // Mensagem disparada.
    public string? MessageTriggered { get; set; }

    // ID da regra acionada.
    public string? TriggeredRuleId { get; set; }
}

// Representa a resposta com dados de um registro de histórico.
public class HistoryRecordResponse
{
    // ID único do registro.
    public string? Id { get; set; }

    // ID do paciente.
    public string? PatientId { get; set; }

    // Nome do paciente.
    public string? PatientName { get; set; }

    // Timestamp da análise.
    public DateTime Timestamp { get; set; }

    // Emoções detectadas.
    public Dictionary<string, double>? EmotionsDetected { get; set; }

    // Emoção dominante.
    public string? DominantEmotion { get; set; }

    // Percentual da emoção dominante.
    public double DominantPercentage { get; set; }

    // Mensagem disparada.
    public string? MessageTriggered { get; set; }

    // Construtor que inicializa a partir de um objeto HistoryRecord.
    public HistoryRecordResponse(HistoryRecord record, string? patientName = null)
    {
        Id = record.Id;
        PatientId = record.PatientId;
        PatientName = patientName; // Agora vem do parâmetro, não do record
        Timestamp = record.Timestamp;
        EmotionsDetected = record.EmotionsDetected;
        DominantEmotion = record.DominantEmotion;
        DominantPercentage = record.DominantPercentage;
        MessageTriggered = record.MessageTriggered;
    }
}

// Representa estatísticas de um paciente para o dashboard.
public class PatientStatistics
{
    // ID do paciente.
    public string? PatientId { get; set; }

    // Nome do paciente.
    public string? PatientName { get; set; }

    // Início do período analisado (últimas 24 horas).
    public DateTime StartPeriod { get; set; }

    // Fim do período analisado (agora).
    public DateTime EndPeriod { get; set; }

    // Total de análises no período.
    public int TotalAnalyses { get; set; }

    // Frequência de cada emoção detectada.
    public Dictionary<string, int>? EmotionFrequency { get; set; }

    // Frequência de cada mensagem disparada.
    public Dictionary<string, int>? MessageFrequency { get; set; }

    // Emoção mais frequente.
    public string? MostFrequentEmotion { get; set; }

    // Mensagem mais disparada.
    public string? MostFrequentMessage { get; set; }

    // Tendências horárias das emoções.
    public List<EmotionTrend>? EmotionTrends { get; set; }
}

// Representa a tendência de emoções por hora.
public class EmotionTrend
{
    // Hora do dia (ex: "14:00").
    public string? Hour { get; set; }

    // Média dos percentuais de cada emoção na hora.
    public Dictionary<string, double>? AverageEmotions { get; set; }

    // Número de análises nesta hora.
    public int AnalysisCount { get; set; }
}

// Representa filtros para consulta de histórico.
public class HistoryFilter
{
    // ID do paciente (opcional).
    public string? PatientId { get; set; }

    // ID do cuidador (opcional, para filtro de segurança).
    public string? CuidadorId { get; set; }

    // Data de início (opcional).
    public DateTime? StartDate { get; set; }

    // Data de fim (opcional).
    public DateTime? EndDate { get; set; }

    // Emoção dominante (opcional).
    public string? DominantEmotion { get; set; }

    // Indica se deve filtrar apenas registros com mensagem (opcional).
    public bool? HasMessage { get; set; }

    // Número da página (padrão: 1).
    public int PageNumber { get; set; } = 1;

    // Tamanho da página (padrão: 50).
    public int PageSize { get; set; } = 50;
}
