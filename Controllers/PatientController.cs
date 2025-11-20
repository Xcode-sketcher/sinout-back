using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

// Controlador para pacientes.
[ApiController]
[Route("api/patients")]
[Authorize]
[EnableRateLimiting("limite-api")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    // Construtor que injeta o serviço de pacientes.
    public PatientController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    // Método para criar novo paciente.
    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] PatientRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var response = await _patientService.CreatePatientAsync(request, userId, userRole);
            return CreatedAtAction(nameof(GetPatientById), new { id = response.Id }, response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter paciente por ID.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientById(int id)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var response = await _patientService.GetPatientByIdAsync(id, userId, userRole);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Método para listar pacientes.
    [HttpGet]
    public async Task<IActionResult> GetPatients()
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            if (userRole == UserRole.Admin.ToString())
            {
                var allPatients = await _patientService.GetAllPatientsAsync();
                return Ok(allPatients);
            }
            else
            {
                var myPatients = await _patientService.GetPatientsByCuidadorAsync(userId);
                return Ok(myPatients);
            }
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para listar pacientes por cuidador (apenas admin).
    [HttpGet("cuidador/{cuidadorId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPatientsByCuidador(int cuidadorId)
    {
        try
        {
            var patients = await _patientService.GetPatientsByCuidadorAsync(cuidadorId);
            return Ok(patients);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para atualizar paciente.
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePatient(int id, [FromBody] PatientRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var response = await _patientService.UpdatePatientAsync(id, request, userId, userRole);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para deletar paciente (soft delete).
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            await _patientService.DeletePatientAsync(id, userId, userRole);
            return Ok(new { message = "Paciente desativado com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
