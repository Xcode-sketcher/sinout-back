// ============================================================
// 👥 SERVIÇO DE USUÁRIOS - O GERENTE DE RECURSOS HUMANOS
// ============================================================
// Analogia RPG: Este é o "Mestre de Guilda"!
// Ele gerencia todos os membros da guilda (usuários): recruta novos,
// promove, rebaixa, e mantém registro de todos os heróis.
//
// Analogia da Cozinha: É o "Gerente de Recursos Humanos"!
// Contrata funcionários, gerencia escalas, atualiza cargos.
//
// Responsabilidades:
// 1. CRUD de usuários (Create, Read, Update, Delete)
// 2. Validar permissões antes de modificar
// 3. Coordenar com Repository para acessar banco
// 4. Aplicar regras de negócio (ex: só Admin pode criar Admin)
//
// Diferença entre Service e Repository:
// - Service = Lógica de negócio (regras, validações, orquestração)
// - Repository = Acesso direto ao banco (queries, CRUD simples)
// ============================================================

using APISinout.Data;
using APISinout.Models;

namespace APISinout.Services;

public class UserService : IUserService
{
    // 📚 INVENTÁRIO: O livro de registros de membros
    private readonly IUserRepository _userRepository;

    // 🏗️ CONSTRUTOR
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Receita: Listar todos os usuários (como verificar o inventário)
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    // Receita: Pegar um usuário específico (como buscar um ingrediente na prateleira)
    public async Task<User> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new Exception("User not found"); // Ingrediente não encontrado!
        return user;
    }

    // Receita: Buscar usuário por email (como procurar um ingrediente pelo rótulo)
    public async Task<User> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) throw new Exception("User not found");
        return user;
    }

    // Receita: Criar um novo usuário (como preparar um novo ingrediente)
    public async Task<User> CreateUserAsync(CreateUserRequest request, string createdBy)
    {
        // Misturar os ingredientes para fazer o novo usuário
        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), // Gerar ObjectId
            UserId = await _userRepository.GetNextUserIdAsync(), // ID numérico sequencial
            Name = request.Name,
            Email = request.Email,
            DataCadastro = DateTime.UtcNow,
            Status = true,
            Role = request.Role ?? "Client", // Tipo padrão se não especificado
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // Temperar
            CreatedBy = createdBy // Quem preparou
        };
        
        // Guardar na despensa
        await _userRepository.CreateUserAsync(user);
        return user; // Servir o ingrediente fresco
    }

    // Receita: Atualizar um usuário (como renovar um ingrediente vencido)
    public async Task UpdateUserAsync(int id, UpdateUserRequest request)
    {
        // Primeiro, pegar o ingrediente atual
        var user = await GetByIdAsync(id);
        
        // Aplicar as mudanças (como trocar o rótulo ou renovar)
        if (request.Name != null) user.Name = request.Name;
        if (request.Email != null) user.Email = request.Email;
        if (request.Status.HasValue) user.Status = request.Status.Value;
        if (request.Role != null) user.Role = request.Role;
        
        // Guardar de volta
        await _userRepository.UpdateUserAsync(id, user);
    }

    // Receita: Deletar um usuário (como jogar fora um ingrediente estragado)
    public async Task DeleteUserAsync(int id)
    {
        // Verificar se existe antes de jogar fora
        await GetByIdAsync(id);
        // Remover da despensa
        await _userRepository.DeleteUserAsync(id);
    }


    public async Task UpdatePatientNameAsync(int userId, string patientName)
    {
        Console.WriteLine($"[UserService] Atualizando nome do paciente - UserId={userId}, Nome='{patientName}'");
        
        // Verificar se usuário existe
        var user = await GetByIdAsync(userId);
        
        // Atualizar no repository
        await _userRepository.UpdatePatientNameAsync(userId, patientName);
        
        Console.WriteLine($"[UserService] Nome do paciente atualizado com sucesso!");
    }
}