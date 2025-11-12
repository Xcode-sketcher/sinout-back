// ============================================================
// 👥 CONTROLADOR DE USUÁRIOS - O GERENCIADOR DE PERSONAGENS
// ============================================================
// Analogia RPG: Este é o "Livro de Heróis" do jogo!
// Aqui gerenciamos todos os personagens (usuários) que existem no sistema.
// Admin é como o "Game Master" - pode criar, editar e remover personagens.
// Usuários comuns só podem ver seu próprio perfil.
//
// Analogia da Cozinha: É o "Cadastro de Funcionários"!
// Admin é o gerente que contrata/demite, e funcionários normais só veem sua própria ficha.
//
// Permissões:
// - 👑 Admin: Pode fazer TUDO (criar, editar, deletar qualquer usuário)
// - 👤 Caregiver: Só pode ver o próprio perfil e atualizar nome do paciente
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

[Authorize]  // 🔐 Todos os endpoints precisam de autenticação
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    // 📚 INVENTÁRIO: O livro de gerenciamento
    private readonly IUserService _userService;

    // 🏗️ CONSTRUTOR
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // ============================================================
    // 📋 MISSÃO 1: LISTAR TODOS OS USUÁRIOS (APENAS ADMIN)
    // ============================================================
    // Analogia RPG: Ver a lista completa de heróis no jogo!
    // Só o Game Master (Admin) pode ver todos os personagens.
    // ============================================================
    [Authorize(Roles = "Admin")]  // 👑 SÓ ADMIN
    [HttpGet]  // Rota: GET /api/users
    public async Task<IActionResult> GetAll()
    {
        // 📜 Buscar todos os usuários e retornar em formato simplificado
        var users = await _userService.GetAllAsync();
        return Ok(users.Select(u => new UserResponse(u)));
    }

    // ============================================================
    // ✨ MISSÃO 2: CRIAR NOVO USUÁRIO (APENAS ADMIN)
    // ============================================================
    // Analogia RPG: Criar novo personagem no jogo!
    // Admin pode criar tanto Admin quanto Caregiver.
    // É como o Game Master adicionando um novo NPC ou jogador.
    // ============================================================
    [Authorize(Roles = "Admin")]  // 👑 SÓ ADMIN
    [HttpPost]  // Rota: POST /api/users
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            // 🎫 Quem está criando este usuário?
            var creatorEmail = AuthorizationHelper.GetCurrentUserEmail(User);
            if (creatorEmail == null)
                return Unauthorized();

            // ✨ Criar o usuário
            var user = await _userService.CreateUserAsync(request, creatorEmail);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse(user));
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 👤 MISSÃO 3: VER MEU PRÓPRIO PERFIL
    // ============================================================
    // Analogia RPG: Abrir a "Ficha do Personagem"!
    // Qualquer usuário pode ver seu próprio perfil.
    // ============================================================
    [HttpGet("me")]  // Rota: GET /api/users/me
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var user = await _userService.GetByIdAsync(userId);
            return Ok(new UserResponse(user));
        }
        catch (AppException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🔍 MISSÃO 4: VER PERFIL DE USUÁRIO ESPECÍFICO (APENAS ADMIN)
    // ============================================================
    // Analogia RPG: Inspecionar ficha de outro personagem!
    // Só o Game Master pode olhar fichas de outros jogadores.
    // ============================================================
    [Authorize(Roles = "Admin")]  // 👑 SÓ ADMIN
    [HttpGet("{id}")]  // Rota: GET /api/users/123
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(new UserResponse(user));
        }
        catch (AppException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ============================================================
    // ✏️ MISSÃO 5: ATUALIZAR USUÁRIO (APENAS ADMIN)
    // ============================================================
    // Analogia RPG: Editar atributos de um personagem!
    // Admin pode mudar nome, email, status (ativo/inativo), cargo, etc.
    // ============================================================
    [Authorize(Roles = "Admin")]  // 👑 SÓ ADMIN
    [HttpPut("{id}")]  // Rota: PUT /api/users/123
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            await _userService.UpdateUserAsync(id, request);
            return Ok(new { message = "Usuário atualizado com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🗑️ MISSÃO 6: DELETAR USUÁRIO (APENAS ADMIN) - SOFT DELETE
    // ============================================================
    // Analogia RPG: "Desativar" personagem (não apagar completamente)!
    // É um soft delete - marca como inativo, mas mantém no banco.
    // Como colocar o personagem "fora de jogo" sem apagar seu histórico.
    // ============================================================
    [Authorize(Roles = "Admin")]  // 👑 SÓ ADMIN
    [HttpDelete("{id}")]  // Rota: DELETE /api/users/123
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            return Ok(new { message = "Usuário desativado com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 📝 MISSÃO 7: ATUALIZAR NOME DO PACIENTE (QUALQUER USUÁRIO AUTENTICADO)
    // ============================================================
    // Analogia RPG: Dar nome ao "NPC Companheiro"!
    // Cada Caregiver pode dar/mudar o nome do paciente que está cuidando.
    // É como personalizar o nome do seu "pet" ou "companheiro" no jogo.
    //
    // Diferente das outras rotas, esta é acessível a qualquer usuário autenticado,
    // não só Admin. Cuidadores podem atualizar o nome do próprio paciente.
    // ============================================================
    [HttpPost("update-patient-name")]  // Rota: POST /api/users/update-patient-name
    public async Task<IActionResult> UpdatePatientName([FromBody] UpdatePatientNameRequest request)
    {
        try
        {
            // 🎫 Quem está atualizando?
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            Console.WriteLine($"[UserController] Atualizando nome do paciente para UserId={userId}, Nome='{request.PatientName}'");
            
            // ✏️ Atualizar o nome
            await _userService.UpdatePatientNameAsync(userId, request.PatientName);
            return Ok(new { message = "Nome do paciente atualizado com sucesso" });
        }
        catch (AppException ex)
        {
            Console.WriteLine($"[UserController] Erro ao atualizar nome do paciente: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 👨‍⚕️ MISSÃO 8: LISTAR TODOS OS CUIDADORES (APENAS ADMIN)
    // ============================================================
    // Analogia RPG: Ver lista de todos os "Healers" (Curandeiros)!
    // Filtra a lista de usuários para mostrar apenas os Caregivers.
    // Útil para Admin ver todos os cuidadores cadastrados.
    // ============================================================
    [Authorize(Roles = "Admin")]  // 👑 SÓ ADMIN
    [HttpGet("caregivers")]  // Rota: GET /api/users/caregivers
    public async Task<IActionResult> GetAllCaregivers()
    {
        // 📜 Buscar todos e filtrar apenas Caregivers
        var users = await _userService.GetAllAsync();
        var caregivers = users.Where(u => u.Role == UserRole.Caregiver.ToString()).Select(u => new UserResponse(u));
        return Ok(caregivers);
    }
}
