// --- MODELO DE USUÁRIO: O PRATO PRINCIPAL ---
// Analogia da cozinha: O User é como o "prato principal" do restaurante!
// É a entidade central que representa um usuário completo, com todos os ingredientes
// necessários para servi-lo corretamente aos clientes.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

public class User
{
    [BsonId] // O "código de barras" único do prato
    [BsonRepresentation(BsonType.Int32)] // Como armazenar no freezer
    public int Id { get; set; } // Número de identificação do prato
    
    [BsonElement("nome")] // Rótulo no freezer
    public string? Name { get; set; } // Nome do prato
    
    [BsonElement("email")] // Outro rótulo
    public string? Email { get; set; } // Ingrediente identificador
    
    [BsonElement("data_cadastro")] // Data de validade
    public DateTime DataCadastro { get; set; } // Quando foi preparado
    
    [BsonElement("status")] // Se está disponível
    public bool Status { get; set; } // Prato ativo ou não
    
    [BsonElement("role")] // Tipo de prato
    public string? Role { get; set; } // Cliente, Admin, etc.
    
    public string? PasswordHash { get; set; } // Receita secreta (senha criptografada)
    
    public string? CreatedBy { get; set; } // Quem preparou o prato
}