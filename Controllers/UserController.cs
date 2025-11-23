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
    private readonly IPatientService _patientService;

    // Construtor que injeta o serviço de usuários e pacientes.
    public UserController(IUserService userService, IPatientService patientService)
    {
        _userService = userService;
        _patientService = patientService;
    }

    // Método para obter perfil do usuário logado.
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var user = await _userService.GetByIdAsync(userId);
            
            // Buscar paciente associado ao cuidador
            // Buscar paciente associado ao cuidador
            string? patientId = null;
            string? patientName = null;
            try 
            {
                var patients = await _patientService.GetPatientsByCuidadorAsync(userId);
                var patient = patients.FirstOrDefault();
                if (patient != null)
                {
                    patientId = patient.Id;
                    patientName = patient.Name;
                }
            }
            catch 
            {
                // Se não encontrar paciente ou der erro, apenas segue sem ID
            }

            return Ok(new UserResponse(user, patientId, patientName));
        }
        catch (AppException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
