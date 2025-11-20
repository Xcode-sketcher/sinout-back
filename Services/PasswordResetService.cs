using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using System.Security.Cryptography;

namespace APISinout.Services;

/// <summary>
/// Interface para o serviço de redefinição de senha.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Solicita a redefinição de senha.
    /// </summary>
    Task<MessageResponse> RequestPasswordResetAsync(ForgotPasswordRequest request);

    /// <summary>
    /// Reenvia o código de redefinição.
    /// </summary>
    Task<MessageResponse> ResendResetCodeAsync(ResendResetCodeRequest request);

    /// <summary>
    /// Redefine a senha.
    /// </summary>
    Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Altera a senha.
    /// </summary>
    Task<MessageResponse> ChangePasswordAsync(ChangePasswordRequest request, int userId);
}

public class PasswordResetService : IPasswordResetService
{
    private readonly IPasswordResetRepository _resetRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IPasswordResetRepository resetRepository, 
        IUserRepository userRepository,
        IEmailService emailService,
        IRateLimitService rateLimitService,
        ILogger<PasswordResetService> logger)
    {
        _resetRepository = resetRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    // Solicita a redefinição de senha.
    public async Task<MessageResponse> RequestPasswordResetAsync(ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            throw new AppException("Email é obrigatório");

        var email = request.Email.ToLower().Trim();

        // Rate Limiting - Máximo 3 tentativas a cada 15 minutos por email
        if (_rateLimitService.IsRateLimited($"reset:{email}", maxAttempts: 3, windowMinutes: 15))
        {
            _logger.LogWarning("[PasswordReset] Rate limit excedido para {Email}", email);
            throw new AppException("Muitas tentativas. Tente novamente em alguns minutos.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        
        // Por segurança, sempre retornar sucesso mesmo se usuário não existir
        if (user == null)
        {
            _logger.LogWarning("[PasswordReset] Tentativa de reset para email não existente: {Email}", email);
            return new MessageResponse("Se o email existir, você receberá um código para redefinir sua senha");
        }

        if (!user.Status)
            throw new AppException("Usuário inativo");

        // Registrar tentativa
        _rateLimitService.RecordAttempt($"reset:{email}");

        // Gerar código numérico de 6 dígitos (mais fácil de digitar)
        var code = GenerateNumericCode();
        
        _logger.LogInformation("[PasswordReset] Gerando código para {Email}", user.Email);

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId, // ID numérico do usuário
            Email = user.Email,
            Token = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Expira em 1 hora
            Used = false
        };

        await _resetRepository.CreateTokenAsync(resetToken);

        // Enviar email com o código
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email!, code);
            _logger.LogInformation("[PasswordReset] Email enviado com sucesso para {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PasswordReset] Erro ao enviar email para {Email}", user.Email);
            // Em modo DEV, retornar o código no response
            return new MessageResponse($"Código de redefinição (DEV): {code}. Email não configurado.");
        }
        
        return new MessageResponse("Se o email existir, você receberá um código para redefinir sua senha");
    }

    // Reenvia o código de redefinição.
    public async Task<MessageResponse> ResendResetCodeAsync(ResendResetCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            throw new AppException("Email é obrigatório");

        var email = request.Email.ToLower().Trim();

        // Rate Limiting - Mesmo controle do request inicial
        if (_rateLimitService.IsRateLimited($"reset:{email}", maxAttempts: 3, windowMinutes: 15))
        {
            _logger.LogWarning("[PasswordReset] Rate limit excedido ao reenviar código para {Email}", email);
            throw new AppException("Muitas tentativas. Tente novamente em alguns minutos.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        
        if (user == null)
        {
            _logger.LogWarning("[PasswordReset] Tentativa de reenvio para email não existente: {Email}", email);
            return new MessageResponse("Se o email existir, você receberá um código para redefinir sua senha");
        }

        if (!user.Status)
            throw new AppException("Usuário inativo");

        // Verificar se há token ativo recente (menos de 5 minutos)
        var existingToken = await _resetRepository.GetActiveTokenByUserIdAsync(user.UserId);
        if (existingToken != null && existingToken.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
        {
            var waitTime = 5 - (DateTime.UtcNow - existingToken.CreatedAt).TotalMinutes;
            _logger.LogWarning("[PasswordReset] Tentativa de reenvio muito rápida para {Email}. Aguardar {WaitTime:F1} minutos", email, waitTime);
            throw new AppException($"Aguarde {Math.Ceiling(waitTime)} minuto(s) antes de solicitar um novo código");
        }

        // Registrar tentativa
        _rateLimitService.RecordAttempt($"reset:{email}");

        // Gerar novo código
        var code = GenerateNumericCode();
        
        _logger.LogInformation("[PasswordReset] Reenviando código para {Email}", user.Email);

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            Email = user.Email,
            Token = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Used = false
        };

        await _resetRepository.CreateTokenAsync(resetToken);

        // Enviar email
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email!, code);
            _logger.LogInformation("[PasswordReset] Código reenviado com sucesso para {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PasswordReset] Erro ao reenviar email para {Email}", user.Email);
            return new MessageResponse($"Código de redefinição (DEV): {code}. Email não configurado.");
        }
        
        return new MessageResponse("Código reenviado. Verifique seu email.");
    }

    // Redefine a senha.
    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
            throw new AppException("Token é obrigatório");

        if (string.IsNullOrEmpty(request.NewPassword))
            throw new AppException("Nova senha é obrigatória");

        if (request.NewPassword != request.ConfirmPassword)
            throw new AppException("Senhas não coincidem");

        if (request.NewPassword.Length < 6)
            throw new AppException("Senha deve ter no mínimo 6 caracteres");

        var resetToken = await _resetRepository.GetByTokenAsync(request.Token);
        if (resetToken == null)
            throw new AppException("Token inválido ou expirado");

        var user = await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user == null)
            throw new AppException("Usuário não encontrado");

        // Atualizar senha
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateUserAsync(user.UserId, user);

        // Marcar token como usado
        await _resetRepository.MarkAsUsedAsync(resetToken.Id!);

        // Limpar rate limit após sucesso
        _rateLimitService.ClearAttempts($"reset:{user.Email}");

        // Enviar notificação de senha alterada
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(user.Email!);
            _logger.LogInformation("[PasswordReset] Notificação de senha alterada enviada para {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PasswordReset] Erro ao enviar notificação para {Email}", user.Email);
            // Não falhar - notificação é opcional
        }

        return new MessageResponse("Senha redefinida com sucesso");
    }

    // Altera a senha.
    public async Task<MessageResponse> ChangePasswordAsync(ChangePasswordRequest request, int userId)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword))
            throw new AppException("Senha atual é obrigatória");

        if (string.IsNullOrEmpty(request.NewPassword))
            throw new AppException("Nova senha é obrigatória");

        if (request.NewPassword != request.ConfirmPassword)
            throw new AppException("Senhas não coincidem");

        if (request.NewPassword.Length < 6)
            throw new AppException("Nova senha deve ter no mínimo 6 caracteres");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");

        // Verificar senha atual
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AppException("Senha atual incorreta");

        // Atualizar senha
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateUserAsync(user.UserId, user);

        // Enviar notificação de senha alterada
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(user.Email!);
            _logger.LogInformation("[PasswordReset] Notificação de troca de senha enviada para {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PasswordReset] Erro ao enviar notificação de troca para {Email}", user.Email);
            // Não falhar - notificação é opcional
        }

        return new MessageResponse("Senha alterada com sucesso");
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GenerateNumericCode()
    {
        // Gera código numérico de 6 dígitos (100000 - 999999)
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomNumber = BitConverter.ToUInt32(bytes, 0);
            var code = (randomNumber % 900000) + 100000; // Garante 6 dígitos
            return code.ToString();
        }
    }
}
