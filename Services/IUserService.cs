using APISinout.Models;

namespace APISinout.Services;

// Interface para o serviço de usuários
public interface IUserService
{
    // Obtém todos os usuários
    Task<IEnumerable<User>> GetAllAsync();

    // Obtém um usuário pelo ID
    Task<User> GetByIdAsync(string id);

    // Obtém um usuário pelo email
    Task<User> GetByEmailAsync(string email);

    // Cria um novo usuário
    Task<User> CreateUserAsync(CreateUserRequest request, string createdBy);

    // Atualiza um usuário
    Task UpdateUserAsync(string id, UpdateUserRequest request);

    // Deleta um usuário
    Task DeleteUserAsync(string id);
}