using APISinout.Models;

namespace APISinout.Services;

/// <summary>Interface para o serviço de usuários</summary>
public interface IUserService
{
    /// <summary>Obtém todos os usuários</summary>
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>Obtém um usuário pelo ID</summary>
    Task<User> GetByIdAsync(string id);

    /// <summary>Obtém um usuário pelo email</summary>
    Task<User> GetByEmailAsync(string email);

    /// <summary>Cria um novo usuário</summary>
    Task<User> CreateUserAsync(CreateUserRequest request, string createdBy);

    /// <summary>Atualiza um usuário</summary>
    Task UpdateUserAsync(string id, UpdateUserRequest request);

    /// <summary>Deleta um usuário</summary>
    Task DeleteUserAsync(string id);
}