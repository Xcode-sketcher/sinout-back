using APISinout.Models;

namespace APISinout.Services;

/// <summary>Interface para o serviço de autenticação</summary>
public interface IAuthService
{
    /// <summary>Registra um novo usuário</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>Faz login de um usuário</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>Obtém um usuário pelo ID</summary>
    Task<User> GetUserByIdAsync(string userId);
}