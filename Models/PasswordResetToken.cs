// --- MODELO DE RESET DE SENHA: SEGURANÇA E RECUPERAÇÃO ---
// Sistema seguro para permitir que usuários redefinam suas senhas
// usando tokens temporários com expiração.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

public class PasswordResetToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("id_usuario")]
    public int UserId { get; set; }
    
    [BsonElement("email")]
    public string? Email { get; set; }
    
    [BsonElement("token")]
    public string? Token { get; set; } // Token único gerado
    
    [BsonElement("data_criacao")]
    public DateTime CreatedAt { get; set; }
    
    [BsonElement("data_expiracao")]
    public DateTime ExpiresAt { get; set; } // Expira em 1 hora
    
    [BsonElement("utilizado")]
    public bool Used { get; set; } // Se já foi utilizado
    
    [BsonElement("data_utilizacao")]
    public DateTime? UsedAt { get; set; }
}

// Request para solicitar reset de senha
public class ForgotPasswordRequest
{
    public string? Email { get; set; }
}

// Request para redefinir senha com token
public class ResetPasswordRequest
{
    public string? Token { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
}

// Request para alterar senha (usuário autenticado)
public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
}

// Resposta genérica de sucesso
public class MessageResponse
{
    public string? Message { get; set; }
    public bool Success { get; set; }

    public MessageResponse(string message, bool success = true)
    {
        Message = message;
        Success = success;
    }
}
