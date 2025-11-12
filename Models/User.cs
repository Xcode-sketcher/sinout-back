// --- MODELO DE USUÁRIO: O PRATO PRINCIPAL ---
// Analogia da cozinha: O User é como o "prato principal" do restaurante!
// É a entidade central que representa um usuário completo, com todos os ingredientes
// necessários para servi-lo corretamente aos clientes.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

public class User
{
    [BsonId] // ID principal do MongoDB (ObjectId)
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } // MongoDB ObjectId (_id)
    
    [BsonElement("id_usuario")] // ID sequencial numérico
    public int UserId { get; set; } // ID numérico para facilidade de uso (1, 2, 3...)
    
    [BsonElement("nome")] // Rótulo no freezer
    public string? Name { get; set; } // Nome do prato
    
    [BsonElement("email")] // Outro rótulo
    public string? Email { get; set; } // Ingrediente identificador
    
    [BsonElement("data_cadastro")] // Data de validade
    public DateTime DataCadastro { get; set; } // Quando foi preparado
    
    [BsonElement("status")] // Se está disponível
    public bool Status { get; set; } // Prato ativo ou não
    
    [BsonElement("cargo")] // Tipo de perfil: Admin ou Caregiver (ver UserRole enum)
    public string? Role { get; set; } // Admin, Caregiver (ver UserRole enum)
    
    [BsonElement("password_hash")]
    public string? PasswordHash { get; set; } // Receita secreta (senha criptografada)
    
    [BsonElement("criado_por")]
    public string? CreatedBy { get; set; } // Quem criou o usuário
    
    [BsonElement("ultimo_acesso")]
    public DateTime? LastLogin { get; set; } // Último acesso
    
    [BsonElement("telefone")]
    public string? Phone { get; set; } // Telefone de contato
    
    [BsonElement("data_atualizacao")]
    public DateTime? UpdatedAt { get; set; } // Data da última atualização

    
    [BsonElement("nome_paciente")]
    public string? PatientName { get; set; } // Nome do paciente (1:1 cuidador-paciente)
}