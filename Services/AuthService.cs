// --- SERVIÇO DE AUTENTICAÇÃO: O COZINHEIRO CHEFE ---
// Voltando à analogia da cozinha!
// O AuthService é como o "cozinheiro chefe" responsável pelos pratos de autenticação.
// Ele pega os "ingredientes" (dados do usuário), cozinha (processa) e serve o resultado.
// Aqui fazemos o registro e login, como preparar um prato principal e uma sobremesa.

using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;

namespace APISinout.Services;

public class AuthService : IAuthService
{
    // Os "utensílios" necessários: repositório para buscar ingredientes, config para temperos
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;

    // Preparar os utensílios
    public AuthService(
        IUserRepository userRepository,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _config = config;
    }

    // Receita: Registrar um novo usuário (como fazer um prato novo)
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Verificar se os ingredientes estão frescos (validações básicas)
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Password))
            throw new AppException("Dados inválidos");

        // Verificar se já existe esse prato no cardápio
        if (await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim()) != null)
            throw new AppException("Email já cadastrado");

        // Preparar o prato: misturar os ingredientes
        var user = new User
        {
            Id = await _userRepository.GetNextUserIdAsync(), // Pegar o próximo número da receita
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            DataCadastro = DateTime.UtcNow, // Data de produção
            Status = true, // Prato pronto para servir
            Role = "Client", // Tipo de prato
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // Temperar a senha
            CreatedBy = "self-registration" // Quem fez o prato
        };

        // Colocar no forno (salvar no banco)
        await _userRepository.CreateUserAsync(user);
        
        // Servir o prato com acompanhamento (token)
        return new AuthResponse { 
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config) // O "certificado de qualidade"
        };
    }

    // Receita: Fazer login (como aquecer um prato pronto)
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Verificar ingredientes
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new AppException("Dados inválidos");

        // Buscar o prato no cardápio
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim());
        
        // Verificar se o prato existe e está fresco
        if (user == null || !user.Status || 
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppException("Credenciais inválidas");

        // Servir o prato aquecido
        return new AuthResponse { 
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config)
        };
    }
}