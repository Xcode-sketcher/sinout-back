using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

// Representa um paciente no sistema.
// Corresponde à pessoa com limitações severas (ELA) que é cuidada.
public class Patient
{
    // ID único do paciente.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // Nome do paciente.
    [BsonElement("nome")]
    public string? Name { get; set; }

    // ID do usuário cuidador responsável.
    [BsonElement("id_cuidador")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CuidadorId { get; set; }

    // Data de cadastro do paciente.
    [BsonElement("data_cadastro")]
    public DateTime DataCadastro { get; set; }

    // Informações adicionais sobre o paciente.
    [BsonElement("informacoes_adicionais")]
    public string? AdditionalInfo { get; set; }

    // ID da foto de perfil (índice do avatar pré-definido).
    [BsonElement("foto_perfil")]
    public int? ProfilePhoto { get; set; }
}

// Representa uma solicitação para criar ou atualizar um paciente.
public class PatientRequest
{
    // Nome do paciente.
    public string? Name { get; set; }

    // ID do cuidador (opcional se for cadastro).
    public string? CuidadorId { get; set; }

    // Informações adicionais.
    public string? AdditionalInfo { get; set; }

    // ID da foto de perfil (índice).
    public int? ProfilePhoto { get; set; }
}

// Representa a resposta com dados de um paciente.
public class PatientResponse
{
    // ID único do paciente.
    public string? Id { get; set; }

    // Nome do paciente.
    public string? Name { get; set; }

    // ID do cuidador.
    public string? CuidadorId { get; set; }

    // Nome do cuidador.
    public string? CuidadorName { get; set; }

    // Data de cadastro.
    public DateTime DataCadastro { get; set; }

    // Informações adicionais.
    public string? AdditionalInfo { get; set; }

    // ID da foto de perfil.
    public int? ProfilePhoto { get; set; }

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
        AdditionalInfo = patient.AdditionalInfo;
        ProfilePhoto = patient.ProfilePhoto;
    }
}
