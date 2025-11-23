using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

// Representa o mapeamento de emoções para mensagens.
// Define regras que traduzem emoções detectadas em palavras ou mensagens.
public class EmotionMapping
{
    // ID único do mapeamento no MongoDB.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // ID do usuário cuidador ao qual este mapeamento pertence.
    [BsonElement("id_usuario")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UserId { get; set; }

    // Emoção primária detectada (ex: happy, sad, angry, fear, surprise, neutral).
    [BsonElement("emocao")]
    public string? Emotion { get; set; }

    // Nível de intensidade da emoção ("high" >= 70%, "moderate" >= 40%).
    [BsonElement("nivel_intensidade")]
    public string? IntensityLevel { get; set; }

    // Percentual mínimo para disparar a mensagem (ex: 80.0).
    [BsonElement("percentual_minimo")]
    public double MinPercentage { get; set; }

    // Palavra ou frase a ser exibida quando a regra for acionada.
    [BsonElement("mensagem")]
    public string? Message { get; set; }

    // Prioridade da mensagem (1 ou 2) para ordenar as mensagens por emoção.
    [BsonElement("prioridade")]
    public int Priority { get; set; }

    // Indica se esta regra está ativa.
    [BsonElement("ativo")]
    public bool Active { get; set; }

    // Data de criação do mapeamento.
    [BsonElement("data_criacao")]
    public DateTime CreatedAt { get; set; }

    // Data da última atualização do mapeamento.
    [BsonElement("data_atualizacao")]
    public DateTime? UpdatedAt { get; set; }
}

// Representa uma solicitação para criar ou atualizar um mapeamento de emoção.
public class EmotionMappingRequest
{
    // ID do usuário cuidador.
    public string? UserId { get; set; }

    // Emoção primária (ex: happy, sad, angry, fear, surprise, neutral).
    public string? Emotion { get; set; }

    // Nível de intensidade (high, moderate).
    public string? IntensityLevel { get; set; }

    // Percentual mínimo para disparar.
    public double MinPercentage { get; set; }

    // Mensagem a ser exibida.
    public string? Message { get; set; }

    // Prioridade da mensagem (1 ou 2).
    public int Priority { get; set; }
}

// Representa a resposta com dados de um mapeamento de emoção.
public class EmotionMappingResponse
{
    // ID único do mapeamento.
    public string? Id { get; set; }

    // ID do usuário cuidador.
    public string? UserId { get; set; }

    // Nome do usuário cuidador.
    public string? UserName { get; set; }

    // Emoção primária.
    public string? Emotion { get; set; }

    // Nível de intensidade.
    public string? IntensityLevel { get; set; }

    // Percentual mínimo.
    public double MinPercentage { get; set; }

    // Mensagem associada.
    public string? Message { get; set; }

    // Prioridade da mensagem.
    public int Priority { get; set; }

    // Indica se está ativo.
    public bool Active { get; set; }

    // Data de criação.
    public DateTime CreatedAt { get; set; }

    // Data da última atualização.
    public DateTime? UpdatedAt { get; set; }

    // Construtor padrão para desserialização JSON.
    public EmotionMappingResponse() { }

    // Construtor que inicializa a partir de um objeto EmotionMapping.
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

// Representa uma solicitação de análise de emoção em tempo real.
public class EmotionAnalysisRequest
{
    // ID do paciente.
    public int PatientId { get; set; }

    // Dicionário com as emoções detectadas e seus percentuais (ex: {"happy": 85.5, "sad": 10.2}).
    public Dictionary<string, double>? Emotions { get; set; }

    // Emoção dominante detectada.
    public string? DominantEmotion { get; set; }
}

// Representa a resposta de uma análise de emoção.
public class EmotionAnalysisResponse
{
    // ID do paciente.
    public int PatientId { get; set; }

    // Emoção dominante.
    public string? DominantEmotion { get; set; }

    // Percentual da emoção dominante.
    public double Percentage { get; set; }

    // Mensagem disparada ou null se não houver.
    public string? Message { get; set; }

    // Indica se uma mensagem foi disparada.
    public bool MessageTriggered { get; set; }

    // Timestamp da análise.
    public DateTime Timestamp { get; set; }
}
