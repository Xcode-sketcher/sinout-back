using APISinout.Models;

namespace APISinout.Services;

// Interface para o serviço de autenticação
public interface IAuthService
{
    // Registra um novo usuário
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    // Faz login de um usuário
    Task<AuthResponse> LoginAsync(LoginRequest request);

    // Obtém um usuário pelo ID
    Task<User?> GetUserByIdAsync(string userId);
}