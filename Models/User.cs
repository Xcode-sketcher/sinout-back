using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

// Representa um usuário no sistema.
// Entidade central que representa um usuário completo.
public class User
{
    // ID principal no MongoDB (ObjectId).
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // ID numérico sequencial para facilidade de uso.
    [BsonElement("id_usuario")]
    public int UserId { get; set; }

    // Nome do usuário.
    [BsonElement("nome")]
    public string? Name { get; set; }

    // Email do usuário.
    [BsonElement("email")]
    public string? Email { get; set; }

    // Data de cadastro do usuário.
    [BsonElement("data_cadastro")]
    public DateTime DataCadastro { get; set; }

    // Indica se o usuário está ativo.
    [BsonElement("status")]
    public bool Status { get; set; }

    // Papel do usuário: Admin ou Cuidador.
    [BsonElement("cargo")]
    public string? Role { get; set; }

    // Hash da senha criptografada.
    [BsonElement("password_hash")]
    public string? PasswordHash { get; set; }

    // Quem criou o usuário.
    [BsonElement("criado_por")]
    public string? CreatedBy { get; set; }

    // Data do último acesso.
    [BsonElement("ultimo_acesso")]
    public DateTime? LastLogin { get; set; }

    // Telefone de contato.
    [BsonElement("telefone")]
    public string? Phone { get; set; }

    // Data da última atualização.
    [BsonElement("data_atualizacao")]
    public DateTime? UpdatedAt { get; set; }

    // Nome do paciente associado (1:1 cuidador-paciente).
    [BsonElement("nome_paciente")]
    public string? PatientName { get; set; }

    // Número de tentativas de login falhadas.
    [BsonElement("failed_login_attempts")]
    [BsonDefaultValue(0)]
    public int FailedLoginAttempts { get; set; }

    // Data de fim do bloqueio por tentativas excessivas.
    [BsonElement("lockout_end_date")]
    [BsonIgnoreIfNull]
    public DateTime? LockoutEndDate { get; set; }
}