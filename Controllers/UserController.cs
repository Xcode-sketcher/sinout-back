using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

[Authorize]
[EnableRateLimiting("limite-api")]
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    // Construtor que injeta o serviço de usuários.
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // Método para listar todos os usuários (apenas admin).
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users.Select(u => new UserResponse(u)));
    }

    // Método para criar novo usuário (apenas admin).
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            var creatorEmail = AuthorizationHelper.GetCurrentUserEmail(User);
            if (creatorEmail == null)
                return Unauthorized();

            var user = await _userService.CreateUserAsync(request, creatorEmail);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse(user));
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter perfil do usuário logado.
    [HttpGet("me")]
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

    // Método para obter usuário por ID (apenas admin).
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
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

    // Método para atualizar usuário (apenas admin).
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
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

    // Método para deletar usuário (apenas admin, soft delete).
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
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

    // Método para atualizar nome do paciente.
    [HttpPost("update-patient-name")]
    public async Task<IActionResult> UpdatePatientName([FromBody] UpdatePatientNameRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            Console.WriteLine($"[UserController] Atualizando nome do paciente para UserId={userId}, Nome='{request.PatientName}'");

            await _userService.UpdatePatientNameAsync(userId, request.PatientName);
            return Ok(new { message = "Nome do paciente atualizado com sucesso" });
        }
        catch (AppException ex)
        {
            Console.WriteLine($"[UserController] Erro ao atualizar nome do paciente: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para listar todos os cuidadores (apenas admin).
    [Authorize(Roles = "Admin")]
    [HttpGet("cuidadores")]
    public async Task<IActionResult> GetAllCuidadores()
    {
        var users = await _userService.GetAllAsync();
        var cuidadores = users.Where(u => u.Role == UserRole.Cuidador.ToString()).Select(u => new UserResponse(u));
        return Ok(cuidadores);
    }
}
