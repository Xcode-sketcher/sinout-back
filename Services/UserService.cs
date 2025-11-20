using APISinout.Data;
using APISinout.Models;

namespace APISinout.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Método para listar todos os usuários
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    // Método para obter usuário por ID
    public async Task<User> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new Exception("User not found");
        return user;
    }

    // Receita: Buscar usuário por email (como procurar um ingrediente pelo rótulo)
    public async Task<User> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) throw new Exception("User not found");
        return user;
    }

    // Método para criar um novo usuário
    public async Task<User> CreateUserAsync(CreateUserRequest request, string createdBy)
    {
        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = await _userRepository.GetNextUserIdAsync(),
            Name = request.Name,
            Email = request.Email,
            DataCadastro = DateTime.UtcNow,
            Status = true,
            Role = request.Role ?? "Client",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedBy = createdBy
        };
        
        await _userRepository.CreateUserAsync(user);
        return user;
    }

    // Método para atualizar um usuário
    public async Task UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await GetByIdAsync(id);
        
        if (request.Name != null) user.Name = request.Name;
        if (request.Email != null) user.Email = request.Email;
        if (request.Status.HasValue) user.Status = request.Status.Value;
        if (request.Role != null) user.Role = request.Role;
        
        await _userRepository.UpdateUserAsync(id, user);
    }

    // Método para deletar um usuário
    public async Task DeleteUserAsync(int id)
    {
        await GetByIdAsync(id);
        await _userRepository.DeleteUserAsync(id);
    }

    public async Task UpdatePatientNameAsync(int userId, string patientName)
    {
        Console.WriteLine($"[UserService] Atualizando nome do paciente - UserId={userId}, Nome='{patientName}'");
        
        var user = await GetByIdAsync(userId);
        
        await _userRepository.UpdatePatientNameAsync(userId, patientName);
        
        Console.WriteLine($"[UserService] Nome do paciente atualizado com sucesso!");
    }
}