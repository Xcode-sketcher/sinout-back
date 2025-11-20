// --- MODELO DE PACIENTE: A PESSOA ASSISTIDA ---
// Analogia médica: O Patient representa a pessoa com limitações severas (ELA)
// que está sendo cuidada. É o paciente que terá suas expressões faciais monitoradas
// para facilitar a comunicação através das emoções detectadas.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

public class Patient
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int Id { get; set; } // ID único do paciente
    
    [BsonElement("nome")]
    public string? Name { get; set; } // Nome do paciente
    
    [BsonElement("id_cuidador")]
    public int CuidadorId { get; set; } // ID do usuário cuidador responsável
    
    [BsonElement("data_cadastro")]
    public DateTime DataCadastro { get; set; } // Quando foi cadastrado
    
    [BsonElement("status")]
    public bool Status { get; set; } // Se o paciente está ativo
    
    [BsonElement("informacoes_adicionais")]
    public string? AdditionalInfo { get; set; } // Informações extras sobre o paciente
    
    [BsonElement("foto_perfil")]
    public string? ProfilePhoto { get; set; } // URL ou base64 da foto de perfil
    
    [BsonElement("criado_por")]
    public string? CreatedBy { get; set; } // Quem cadastrou (Admin ou Self)
}

// Modelo para criar/atualizar paciente
public class PatientRequest
{
    public string? Name { get; set; }
    public int? CuidadorId { get; set; } // Opcional se for cadastro
    public string? AdditionalInfo { get; set; }
    public string? ProfilePhoto { get; set; }
}

// Modelo de resposta do paciente
public class PatientResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int CuidadorId { get; set; }
    public string? CuidadorName { get; set; } // Nome do cuidador
    public DateTime DataCadastro { get; set; }
    public bool Status { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? ProfilePhoto { get; set; }
    public string? CreatedBy { get; set; }

    // Construtor sem parâmetros para desserialização JSON
    public PatientResponse() { }

    public PatientResponse(Patient patient, string? cuidadorName = null)
    {
        Id = patient.Id;
        Name = patient.Name;
        CuidadorId = patient.CuidadorId;
        CuidadorName = cuidadorName;
        DataCadastro = patient.DataCadastro;
        Status = patient.Status;
        AdditionalInfo = patient.AdditionalInfo;
        ProfilePhoto = patient.ProfilePhoto;
        CreatedBy = patient.CreatedBy;
    }
}
