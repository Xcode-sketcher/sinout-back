// --- MODELO DE MAPEAMENTO DE EMOÇÕES: AS REGRAS DE COMUNICAÇÃO ---
// Este modelo define as "regras" que traduzem emoções em palavras/mensagens.
// Cada paciente pode ter até 2 palavras configuradas por emoção primária detectada.
// Quando a emoção detectada atingir um percentual mínimo, a palavra correspondente é exibida.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

public class EmotionMapping
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } // ID único do mapeamento (MongoDB ObjectId)
    
    [BsonElement("id_usuario")]
    public int UserId { get; set; } // Cuidador ao qual este mapeamento pertence
    
    [BsonElement("emocao")]
    public string? Emotion { get; set; } // happy, sad, angry, fear, surprise, neutral
    
    [BsonElement("nivel_intensidade")]
    public string? IntensityLevel { get; set; } // "high" (>= 70%), "moderate" (>= 40%)
    
    [BsonElement("percentual_minimo")]
    public double MinPercentage { get; set; } // Percentual mínimo para disparar (ex: 80.0)
    
    [BsonElement("mensagem")]
    public string? Message { get; set; } // A palavra/frase a ser exibida
    
    [BsonElement("prioridade")]
    public int Priority { get; set; } // 1 ou 2 (para ordenar as 2 palavras por emoção)
    
    [BsonElement("ativo")]
    public bool Active { get; set; } // Se esta regra está ativa
    
    [BsonElement("data_criacao")]
    public DateTime CreatedAt { get; set; }
    
    [BsonElement("data_atualizacao")]
    public DateTime? UpdatedAt { get; set; }
}

// Modelo para criar/atualizar mapeamento
public class EmotionMappingRequest
{
    public int UserId { get; set; }
    public string? Emotion { get; set; } // happy, sad, angry, fear, surprise, neutral
    public string? IntensityLevel { get; set; } // high, moderate
    public double MinPercentage { get; set; }
    public string? Message { get; set; }
    public int Priority { get; set; } // 1 ou 2
}

// Modelo de resposta
public class EmotionMappingResponse
{
    public string? Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? Emotion { get; set; }
    public string? IntensityLevel { get; set; }
    public double MinPercentage { get; set; }
    public string? Message { get; set; }
    public int Priority { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public EmotionMappingResponse(EmotionMapping mapping, string? userName = null)
    {
        Id = mapping.Id;
        UserId = mapping.UserId;
        UserName = userName;
        Emotion = mapping.Emotion;
        IntensityLevel = mapping.IntensityLevel;
        MinPercentage = mapping.MinPercentage;
        Message = mapping.Message;
        Priority = mapping.Priority;
        Active = mapping.Active;
        CreatedAt = mapping.CreatedAt;
        UpdatedAt = mapping.UpdatedAt;
    }
}

// Modelo para análise em tempo real (vindo da API Python DeepFace)
public class EmotionAnalysisRequest
{
    public int PatientId { get; set; }
    public Dictionary<string, double>? Emotions { get; set; } // Ex: {"happy": 85.5, "sad": 10.2, ...}
    public string? DominantEmotion { get; set; } // Emoção dominante detectada
}

// Resposta da análise (palavra a ser exibida)
public class EmotionAnalysisResponse
{
    public int PatientId { get; set; }
    public string? DominantEmotion { get; set; }
    public double Percentage { get; set; }
    public string? Message { get; set; } // Palavra/frase a ser exibida ou null se não houver
    public bool MessageTriggered { get; set; }
    public DateTime Timestamp { get; set; }
}
