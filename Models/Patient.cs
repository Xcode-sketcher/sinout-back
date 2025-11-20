using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

// Representa um paciente no sistema.
// Corresponde à pessoa com limitações severas (ELA) que é cuidada.
public class Patient
{
    // ID único do paciente.
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int Id { get; set; }

    // Nome do paciente.
    [BsonElement("nome")]
    public string? Name { get; set; }

    // ID do usuário cuidador responsável.
    [BsonElement("id_cuidador")]
    public int CuidadorId { get; set; }

    // Data de cadastro do paciente.
    [BsonElement("data_cadastro")]
    public DateTime DataCadastro { get; set; }

    // Indica se o paciente está ativo.
    [BsonElement("status")]
    public bool Status { get; set; }

    // Informações adicionais sobre o paciente.
    [BsonElement("informacoes_adicionais")]
    public string? AdditionalInfo { get; set; }

    // URL ou base64 da foto de perfil.
    [BsonElement("foto_perfil")]
    public string? ProfilePhoto { get; set; }

    // Quem cadastrou o paciente (Admin ou Self).
    [BsonElement("criado_por")]
    public string? CreatedBy { get; set; }
}

// Representa uma solicitação para criar ou atualizar um paciente.
public class PatientRequest
{
    // Nome do paciente.
    public string? Name { get; set; }

    // ID do cuidador (opcional se for cadastro).
    public int? CuidadorId { get; set; }

    // Informações adicionais.
    public string? AdditionalInfo { get; set; }

    // Foto de perfil.
    public string? ProfilePhoto { get; set; }
}

// Representa a resposta com dados de um paciente.
public class PatientResponse
{
    // ID único do paciente.
    public int Id { get; set; }

    // Nome do paciente.
    public string? Name { get; set; }

    // ID do cuidador.
    public int CuidadorId { get; set; }

    // Nome do cuidador.
    public string? CuidadorName { get; set; }

    // Data de cadastro.
    public DateTime DataCadastro { get; set; }

    // Indica se está ativo.
    public bool Status { get; set; }

    // Informações adicionais.
    public string? AdditionalInfo { get; set; }

    // Foto de perfil.
    public string? ProfilePhoto { get; set; }

    // Quem criou.
    public string? CreatedBy { get; set; }

    // Construtor padrão para desserialização JSON.
    public PatientResponse() { }

    // Construtor que inicializa a partir de um objeto Patient.
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
