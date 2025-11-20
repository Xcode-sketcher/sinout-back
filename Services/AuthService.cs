using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;


namespace APISinout.Services;

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

    // Método para registrar um novo usuário
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Password))
            throw new AppException("Dados inválidos");

        if (!IsValidEmail(request.Email))
            throw new AppException("Email inválido");

        if (request.Password.Length < 8)
            throw new AppException("Senha deve ter no mínimo 8 caracteres");

        if (await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim()) != null)
            throw new AppException("Email já cadastrado");

        var role = string.IsNullOrEmpty(request.Role) ? UserRole.Cuidador.ToString() : request.Role;
        if (role != UserRole.Admin.ToString() && role != UserRole.Cuidador.ToString())
            throw new AppException($"Role inválido. Valores permitidos: {UserRole.Admin}, {UserRole.Cuidador}");

        if (role == UserRole.Admin.ToString())
            throw new AppException("Não é possível auto-registrar como Admin");

        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = await _userRepository.GetNextUserIdAsync(),
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            Phone = request.Phone?.Trim(),
            PatientName = request.PatientName?.Trim(),
            DataCadastro = DateTime.UtcNow,
            Status = true,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedBy = "cadastro",
            LastLogin = null
        };

        await _userRepository.CreateUserAsync(user);
        
        return new AuthResponse { 
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config)
        };
    }

    // Método para fazer login
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new AppException("Email e senha são obrigatórios");

        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim());

        if (user == null || !user.Status)
        {
            throw new AppException("Credenciais inválidas");
        }

        if (user.LockoutEndDate.HasValue && user.LockoutEndDate > DateTime.UtcNow)
        {
            var timeLeft = (user.LockoutEndDate.Value - DateTime.UtcNow).Minutes;
            throw new AppException($"Conta bloqueada. Tente novamente em {timeLeft + 1} minutos.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            const int MAX_ATTEMPTS = 5;

            if (user.FailedLoginAttempts >= MAX_ATTEMPTS)
            {
                user.LockoutEndDate = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }

            await _userRepository.UpdateUserAsync(user.UserId, user);

            throw new AppException("Credenciais inválidas");
        }

        if (string.IsNullOrEmpty(user.Role))
        {
            user.Role = UserRole.Cuidador.ToString();
        }

        user.LastLogin = DateTime.UtcNow;

        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;

        await _userRepository.UpdateUserAsync(user.UserId, user);

        return new AuthResponse
        {
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config)
        };
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    // Método para obter usuário por ID
    public async Task<User> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");
        
        return user;
    }
}