using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int Id { get; set; }
    
    [BsonElement("nome")]
    public string? Name { get; set; }
    
    [BsonElement("email")]
    public string? Email { get; set; }
    
    [BsonElement("data_cadastro")]
    public DateTime DataCadastro { get; set; }
    
    [BsonElement("status")]
    public bool Status { get; set; }
    
    [BsonElement("role")]
    public string? Role { get; set; }
    
    public string? PasswordHash { get; set; }
    
    public string? CreatedBy { get; set; }
}