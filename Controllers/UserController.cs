// --- CONTROLADOR DE USUÁRIOS: O MERCADO DO JOGO ---
// Continuando a analogia do jogo RPG!
// O UserController é como o "mercado" ou "guilda" onde os jogadores gerenciam seus personagens.
// Aqui os admins podem ver, criar, atualizar e deletar jogadores (como um mestre do jogo).
// É a área VIP onde só os admins têm acesso!

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;

namespace APISinout.Controllers;

[Authorize] // Só jogadores autorizados podem entrar aqui
[ApiController]
[Route("api/users")] // Endereço da guilda
public class UserController : ControllerBase
{
    // O "assistente pessoal" para gerenciar usuários
    private readonly IUserService _userService;

    // Equipar o assistente
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // Missão: Ver todos os jogadores (só para admins)
    [Authorize(Roles = "Admin")] // Precisa ser admin para ver a lista completa
    [HttpGet] // GET é como "olhar o mapa"
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users.Select(u => new UserResponse(u))); // Retornar a lista "mascarada"
    }

    // Missão: Criar um novo jogador (só para admins)
    [Authorize(Roles = "Admin")]
    [HttpPost] // POST = "construir algo novo"
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        // Verificar se o jogador está logado
        if (User.Identity?.Name == null) return Unauthorized();
        
        var user = await _userService.CreateUserAsync(request, User.Identity.Name);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse(user));
        // "Novo personagem criado!" - retorna onde ver os detalhes
    }

    // Missão: Ver o próprio perfil (qualquer jogador logado)
    [HttpGet("me")] // Rota especial para "eu mesmo"
    public async Task<IActionResult> GetCurrentUser()
    {
        // Verificar login
        if (User.Identity?.Name == null) return Unauthorized();
        
        var user = await _userService.GetByEmailAsync(User.Identity.Name);
        return Ok(new UserResponse(user)); // "Aqui está seu personagem!"
    }

    // Missão: Ver detalhes de um jogador específico (só admins)
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")] // {id} é como escolher um item específico no inventário
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return Ok(new UserResponse(user)); // "Aqui estão os detalhes!"
    }

    // Missão: Atualizar um jogador (só admins)
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")] // PUT = "modificar algo existente"
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        await _userService.UpdateUserAsync(id, request);
        return NoContent(); // "Atualização feita, sem novidades!"
    }

    // Missão: Deletar um jogador (só admins) - CUIDADO!
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")] // DELETE = "destruir algo"
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent(); // "Personagem removido do jogo!"
    }
}