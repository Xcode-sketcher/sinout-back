// ============================================================
// 🏥 CONTROLADOR DE PACIENTES - O LIVRO DE PACIENTES
// ============================================================
// Analogia RPG: Este é o "Livro de Missões" onde cada missão representa um paciente!
// Cada Cuidador (jogador) tem suas próprias missões (pacientes) para cuidar.
// O Admin (Game Master) pode ver e gerenciar todas as missões de todos os jogadores.
//
// Analogia Médica: É o "Prontuário Médico"!
// Cada paciente tem seu prontuário com informações importantes.
// Médicos (cuidadores) acessam prontuários dos seus pacientes,
// e o diretor do hospital (admin) pode acessar qualquer prontuário.
//
// Regras de acesso:
// - 👑 Admin: Pode gerenciar TODOS os pacientes
// - 👨‍⚕️ Cuidador: Só pode gerenciar seus PRÓPRIOS pacientes
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]  // 🔐 Todos os endpoints exigem autenticação
[EnableRateLimiting("limite-api")]
public class PatientController : ControllerBase
{
    // 📋 INVENTÁRIO: O livro de prontuários
    private readonly IPatientService _patientService;

    // 🏗️ CONSTRUTOR
    public PatientController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    // ============================================================
    // ✨ MISSÃO 1: CRIAR NOVO PACIENTE
    // ============================================================
    // Analogia RPG: Aceitar uma nova missão!
    // Cuidador pode criar paciente para si mesmo.
    // Admin pode criar paciente e atribuir a qualquer cuidador.
    //
    // Analogia Médica: Admitir novo paciente no hospital!
    // ============================================================
    [HttpPost]  // Rota: POST /api/patients
    public async Task<IActionResult> CreatePatient([FromBody] PatientRequest request)
    {
        try
        {
            // 🎫 Quem está criando?
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // ✨ Criar paciente
            var response = await _patientService.CreatePatientAsync(request, userId, userRole);
            return CreatedAtAction(nameof(GetPatientById), new { id = response.Id }, response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🔍 MISSÃO 2: VER DETALHES DE UM PACIENTE
    // ============================================================
    // Analogia RPG: Abrir detalhes de uma missão específica!
    // Só pode ver se for seu paciente (ou se for Admin).
    // ============================================================
    [HttpGet("{id}")]  // Rota: GET /api/patients/123
    public async Task<IActionResult> GetPatientById(int id)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 📖 Buscar paciente (com validação de permissão)
            var response = await _patientService.GetPatientByIdAsync(id, userId, userRole);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ============================================================
    // 📋 MISSÃO 3: LISTAR PACIENTES
    // ============================================================
    // Analogia RPG: Ver lista de missões!
    // - Admin vê TODAS as missões de TODOS os jogadores
    // - Cuidador vê apenas SUAS próprias missões
    // ============================================================
    [HttpGet]  // Rota: GET /api/patients
    public async Task<IActionResult> GetPatients()
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            if (userRole == UserRole.Admin.ToString())
            {
                // 👑 Admin: ver tudo
                var allPatients = await _patientService.GetAllPatientsAsync();
                return Ok(allPatients);
            }
            else
            {
                // 👨‍⚕️ Cuidador: ver apenas os seus
                var myPatients = await _patientService.GetPatientsByCuidadorAsync(userId);
                return Ok(myPatients);
            }
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 👨‍⚕️ MISSÃO 4: LISTAR PACIENTES DE UM CUIDADOR ESPECÍFICO (APENAS ADMIN)
    // ============================================================
    // Analogia RPG: Ver as missões de um jogador específico!
    // Só o Game Master (Admin) pode fazer isso.
    // ============================================================
    [HttpGet("cuidador/{cuidadorId}")]  // Rota: GET /api/patients/cuidador/123
    [Authorize(Roles = "Admin")]  // 👑 SÓ ADMIN
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

    // ============================================================
    // ✏️ MISSÃO 5: ATUALIZAR PACIENTE
    // ============================================================
    // Analogia RPG: Editar detalhes de uma missão!
    // Só pode editar se for seu paciente (ou se for Admin).
    // ============================================================
    [HttpPut("{id}")]  // Rota: PUT /api/patients/123
    public async Task<IActionResult> UpdatePatient(int id, [FromBody] PatientRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 🔄 Atualizar paciente
            var response = await _patientService.UpdatePatientAsync(id, request, userId, userRole);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🗑️ MISSÃO 6: DELETAR PACIENTE (SOFT DELETE)
    // ============================================================
    // Analogia RPG: "Completar" ou "Cancelar" uma missão!
    // Na verdade não apaga, só marca como inativo (soft delete).
    // É como arquivar um prontuário médico ao invés de destruir.
    // ============================================================
    [HttpDelete("{id}")]  // Rota: DELETE /api/patients/123
    public async Task<IActionResult> DeletePatient(int id)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 🗑️ Desativar paciente
            await _patientService.DeletePatientAsync(id, userId, userRole);
            return Ok(new { message = "Paciente desativado com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
