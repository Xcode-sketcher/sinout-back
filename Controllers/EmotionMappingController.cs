using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

// Controlador para mapeamento de emoções.
[ApiController]
[Route("api/emotion-mappings")]
[Authorize]
[EnableRateLimiting("limite-api")]
public class EmotionMappingController : ControllerBase
{
    private readonly IEmotionMappingService _mappingService;

    // Construtor que injeta o serviço de mapeamento.
    public EmotionMappingController(IEmotionMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    // Método para criar novo mapeamento.
    [HttpPost]
    public async Task<IActionResult> CreateMapping([FromBody] EmotionMappingRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            if (request.UserId == 0)
                request.UserId = userId;

            var response = await _mappingService.CreateMappingAsync(request, userId, userRole);
            return CreatedAtAction(nameof(GetMappingsByUser), new { userId = response.UserId }, response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter mapeamentos por usuário.
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetMappingsByUser(int userId)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var mappings = await _mappingService.GetMappingsByUserAsync(userId, currentUserId, userRole);
            return Ok(mappings);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter mapeamentos do usuário logado.
    [HttpGet("my-rules")]
    public async Task<IActionResult> GetMyMappings()
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var mappings = await _mappingService.GetMappingsByUserAsync(userId, userId, userRole);
            return Ok(mappings);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para atualizar mapeamento.
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMapping(string id, [FromBody] EmotionMappingRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var response = await _mappingService.UpdateMappingAsync(id, request, userId, userRole);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para deletar mapeamento.
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMapping(string id)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            await _mappingService.DeleteMappingAsync(id, userId, userRole);
            return Ok(new { message = "Mapeamento removido com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
