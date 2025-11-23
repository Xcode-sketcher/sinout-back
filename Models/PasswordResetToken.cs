using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace APISinout.Models;

// Representa um token para reset de senha.
// Sistema seguro para permitir redefinição de senhas com expiração.
public class PasswordResetToken
{
    // ID único do token no MongoDB.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // ID do usuário associado.
    [BsonElement("id_usuario")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UserId { get; set; }

    // Email do usuário.
    [BsonElement("email")]
    public string? Email { get; set; }

    // Token único gerado.
    [BsonElement("token")]
    public string? Token { get; set; }

    // Data de criação do token.
    [BsonElement("data_criacao")]
    public DateTime CreatedAt { get; set; }

    // Data de expiração do token (expira em 30 minutos).
    [BsonElement("data_expiracao")]
    public DateTime ExpiresAt { get; set; }

    // Indica se o token já foi utilizado.
    [BsonElement("utilizado")]
    public bool Used { get; set; }

    // Data de utilização do token.
    [BsonElement("data_utilizacao")]
    public DateTime? UsedAt { get; set; }
}

// Representa uma solicitação para solicitar reset de senha.
public class ForgotPasswordRequest
{
    // Email do usuário.
    public string Email { get; set; } = string.Empty;
}

// Representa uma solicitação para redefinir senha usando token.
public class ResetPasswordRequest
{
    // Token de reset.
    public string Token { get; set; } = string.Empty;

    // Nova senha.
    public string NewPassword { get; set; } = string.Empty;

    // Confirmação da nova senha.
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Representa uma solicitação para alterar senha de usuário autenticado.
public class ChangePasswordRequest
{
    // Senha atual.
    public string CurrentPassword { get; set; } = string.Empty;

    // Nova senha.
    public string NewPassword { get; set; } = string.Empty;

    // Confirmação da nova senha.
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Representa uma resposta genérica de mensagem.
public class MessageResponse
{
    // Mensagem da resposta.
    public string Message { get; set; }

    // Indica se a operação foi bem-sucedida.
    public bool Success { get; set; }

    // Construtor para criar uma resposta de mensagem.
    public MessageResponse(string message, bool success = true)
    {
        Message = message ?? string.Empty;
        Success = success;
    }
}