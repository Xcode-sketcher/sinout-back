// --- SERVIÇO DE REDEFINIÇÃO DE SENHA ---
// Gerencia solicitação e validação de tokens para reset de senha

using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using System.Security.Cryptography;

namespace APISinout.Services;

public interface IPasswordResetService
{
    Task<MessageResponse> RequestPasswordResetAsync(ForgotPasswordRequest request);
    Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<MessageResponse> ChangePasswordAsync(ChangePasswordRequest request, int userId);
}

public class PasswordResetService : IPasswordResetService
{
    private readonly IPasswordResetRepository _resetRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public PasswordResetService(
        IPasswordResetRepository resetRepository, 
        IUserRepository userRepository,
        IEmailService emailService)
    {
        _resetRepository = resetRepository;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<MessageResponse> RequestPasswordResetAsync(ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            throw new AppException("Email é obrigatório");

        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim());
        
        // Por segurança, sempre retornar sucesso mesmo se usuário não existir
        if (user == null)
        {
            Console.WriteLine($"[PasswordReset] ⚠️ Tentativa de reset para email não existente: {request.Email}");
            return new MessageResponse("Se o email existir, você receberá um código para redefinir sua senha");
        }

        if (!user.Status)
            throw new AppException("Usuário inativo");

        // Gerar código numérico de 6 dígitos (mais fácil de digitar)
        var code = GenerateNumericCode();
        
        Console.WriteLine($"[PasswordReset] Gerando código para {user.Email}: {code}");

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
            Console.WriteLine($"[PasswordReset] ✅ Email enviado com sucesso para {user.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordReset] ❌ Erro ao enviar email: {ex.Message}");
            // Em modo DEV, retornar o código no response
            return new MessageResponse($"Código de redefinição (DEV): {code}. Email não configurado.");
        }
        
        return new MessageResponse("Se o email existir, você receberá um código para redefinir sua senha");
    }

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

        return new MessageResponse("Senha redefinida com sucesso");
    }

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
