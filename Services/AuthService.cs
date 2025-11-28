using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using Microsoft.Extensions.Logging;


namespace APISinout.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientService _patientService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService>? _logger;

    public AuthService(
        IUserRepository userRepository,
        IPatientService patientService,
        IConfiguration config,
        ILogger<AuthService>? logger = null)
    {
        _userRepository = userRepository;
        _patientService = patientService;
        _config = config;
        _logger = logger;
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

        // Validar role se fornecida
        if (!string.IsNullOrEmpty(request.Role) && 
            request.Role != UserRole.Cuidador.ToString() && 
            request.Role != "Admin")
        {
             throw new AppException("Role inválida");
        }

        // Sistema agora só suporta Cuidador (default) ou Admin (se solicitado explicitamente)
        var role = !string.IsNullOrEmpty(request.Role) ? request.Role : UserRole.Cuidador.ToString();

        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            Phone = request.Phone?.Trim(),
            DataCadastro = DateTime.UtcNow,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedBy = "auto_registration",
            LastLogin = null
        };

        await _userRepository.CreateUserAsync(user);
        _logger?.LogInformation("User registered: Email={Email}", user.Email);

        // Sempre cria um paciente automaticamente durante o registro
        if (!string.IsNullOrEmpty(request.PatientName))
        {
            var patientRequest = new PatientRequest
            {
                Name = request.PatientName
            };
            
            // Cria o paciente associado a este novo usuário (Cuidador)
            await _patientService.CreatePatientAsync(patientRequest, user.Id, user.Role);
        }
        
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

        if (user == null)
        {
            _logger?.LogWarning("Login failed: user not found for email {Email}", request.Email);
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

            await _userRepository.UpdateUserAsync(user.Id!, user);

            _logger?.LogWarning("Login failed: invalid password for {Email}", request.Email);
            throw new AppException("Credenciais inválidas");
        }

        if (string.IsNullOrEmpty(user.Role))
        {
            user.Role = UserRole.Cuidador.ToString();
        }

        user.LastLogin = DateTime.UtcNow;

        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;

        await _userRepository.UpdateUserAsync(user.Id!, user);
        _logger?.LogInformation("User logged in: Email={Email}", user.Email);

        return new AuthResponse
        {
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config, _logger)
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
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }
}