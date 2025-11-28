using APISinout.Data;
using Microsoft.Extensions.Logging;
using APISinout.Helpers;
using APISinout.Models;

namespace APISinout.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService>? _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService>? logger = null)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    // Método para listar todos os usuários
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    // Método para obter usuário por ID
    public async Task<User> GetByIdAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger?.LogWarning("UserService.GetByIdAsync: user not found. Id={UserId}", id);
            throw new AppException("Usuário não encontrado");
        }
        return user;
    }

    // Receita: Buscar usuário por email (como procurar um ingrediente pelo rótulo)
    public async Task<User> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger?.LogWarning("UserService.GetByEmailAsync: user not found. Email={Email}", email);
            throw new AppException("Usuário não encontrado");
        }
        return user;
    }

    // Método para criar um novo usuário
    public async Task<User> CreateUserAsync(CreateUserRequest request, string createdBy)
    {
        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = request.Name,
            Email = request.Email,
            DataCadastro = DateTime.UtcNow,
            Role = request.Role ?? "Client",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedBy = createdBy
        };
        
        await _userRepository.CreateUserAsync(user);
        return user;
    }

    // Método para atualizar um usuário
    public async Task UpdateUserAsync(string id, UpdateUserRequest request)
    {
        var user = await GetByIdAsync(id);
        
        if (request.Name != null) user.Name = request.Name;
        if (request.Email != null) user.Email = request.Email;
        if (request.Role != null) user.Role = request.Role;
        
        await _userRepository.UpdateUserAsync(id, user);
    }

    // Método para deletar um usuário
    public async Task DeleteUserAsync(string id)
    {
        await GetByIdAsync(id);
        await _userRepository.DeleteUserAsync(id);
    }
}