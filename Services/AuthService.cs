namespace APISinout.Services;

using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Password))
            throw new AppException("Dados inválidos");

        if (await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim()) != null)
            throw new AppException("Email já cadastrado");

        var user = new User
        {
            Id = await _userRepository.GetNextUserIdAsync(),
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            DataCadastro = DateTime.UtcNow,
            Status = true,
            Role = "Client",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedBy = "self-registration"
        };

        await _userRepository.CreateUserAsync(user);
        return new AuthResponse { 
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new AppException("Dados inválidos");

        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim());
        
        if (user == null || !user.Status || 
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppException("Credenciais inválidas");

        return new AuthResponse { 
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config)
        };
    }
}