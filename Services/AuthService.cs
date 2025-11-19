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

        // Validar formato de email
        if (!IsValidEmail(request.Email))
            throw new AppException("Email inválido");

        // Validar senha forte
        if (request.Password.Length < 8)
            throw new AppException("Senha deve ter no mínimo 8 caracteres");

        // Verificar se já existe esse email no banco
        if (await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim()) != null)
            throw new AppException("Email já cadastrado");

        // Determinar Role (apenas Admin pode criar outros Admins)
        var role = string.IsNullOrEmpty(request.Role) ? UserRole.Cuidador.ToString() : request.Role;
        if (role != UserRole.Admin.ToString() && role != UserRole.Cuidador.ToString())
            throw new AppException($"Role inválido. Valores permitidos: {UserRole.Admin}, {UserRole.Cuidador}");

        // Por segurança, registro público só pode criar Cuidador
        // Para criar Admin, deve ser através de endpoint protegido
        if (role == UserRole.Admin.ToString())
            throw new AppException("Não é possível auto-registrar como Admin");

        // Preparar o usuário
        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), // Gerar ObjectId
            UserId = await _userRepository.GetNextUserIdAsync(), // ID numérico sequencial
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

        // Salvar no banco
        await _userRepository.CreateUserAsync(user);
        
        // Retornar resposta com token
        return new AuthResponse { 
            User = new UserResponse(user),
            Token = JwtHelper.GenerateToken(user, _config)
        };
    }

    // Receita: Fazer login
    // Receita: Fazer login (VERSÃO ATUALIZADA COM SEGURANÇA)
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Verificar campos obrigatórios (igual ao original)
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new AppException("Email e senha são obrigatórios");

        // Buscar usuário (igual ao original)
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim());

        // --- 1. NOVA VERIFICAÇÃO DE SEGURANÇA (USUÁRIO EXISTE?) ---
        // Se o email não existe, lançamos o erro padrão.
        // Fazemos isso ANTES de checar a senha para não dar erro de "usuário nulo".
        if (user == null || !user.Status)
        {
            throw new AppException("Credenciais inválidas");
        }

        // --- 2. NOVA VERIFICAÇÃO DE SEGURANÇA (CONTA BLOQUEADA?) ---
        // O "prato" está "queimado" (bloqueado)?
        // Verificamos se existe uma data de bloqueio E se essa data AINDA É no futuro.
        if (user.LockoutEndDate.HasValue && user.LockoutEndDate > DateTime.UtcNow)
        {
            // Sim, está bloqueado. Avise o usuário e pare tudo.
            var timeLeft = (user.LockoutEndDate.Value - DateTime.UtcNow).Minutes;
            throw new AppException($"Conta bloqueada. Tente novamente em {timeLeft + 1} minutos.");
        }

        // --- 3. VERIFICAÇÃO DE CREDENCIAIS (SENHA) ---
        // A "receita secreta" (senha) bate?
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // --- 4. LÓGICA DE FALHA (SENHA INCORRETA!) ---
            // Anotar a falha no "rótulo" do prato.
            user.FailedLoginAttempts++;

            // Define o limite (ex: 5 tentativas)
            const int MAX_ATTEMPTS = 5;

            if (user.FailedLoginAttempts >= MAX_ATTEMPTS)
            {
                // Atingiu o limite! Bloqueie a conta por 15 minutos.
                user.LockoutEndDate = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginAttempts = 0; // Zera as tentativas para o próximo ciclo
            }

            // Salva as mudanças (tentativa ou bloqueio) no banco
            // Usamos o UserId (int) porque sua função UpdateUserAsync espera ele!
            await _userRepository.UpdateUserAsync(user.UserId, user);

            // Lança o erro padrão (nunca diga ao usuário que a *senha* estava errada)
            throw new AppException("Credenciais inválidas");
        }

        // --- 5. LÓGICA DE SUCESSO (SENHA CORRETA!) ---

        // Garantir que o usuário tenha uma Role definida (igual ao original)
        if (string.IsNullOrEmpty(user.Role))
        {
            user.Role = UserRole.Cuidador.ToString();
        }

        // Atualizar último login (igual ao original)
        user.LastLogin = DateTime.UtcNow;

        // --- NOVA LÓGICA DE SUCESSO ---
        // Se ele acertou, temos que limpar o histórico de falhas!
        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;

        // Salva TODAS as atualizações de sucesso (LastLogin, FailedAttempts, LockoutEnd)
        await _userRepository.UpdateUserAsync(user.UserId, user);

        // Retornar resposta com token (igual ao original)
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

    // Receita: Obter usuário por ID
    public async Task<User> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");
        
        return user;
    }
}