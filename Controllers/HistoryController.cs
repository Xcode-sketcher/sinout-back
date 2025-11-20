using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

// Controlador para histórico de emoções.
[ApiController]
[Route("api/history")]
[Authorize]
[EnableRateLimiting("limite-api")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;

    // Construtor que injeta o serviço de histórico.
    public HistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    // Método para obter histórico por usuário.
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetHistoryByUser(int userId, [FromQuery] int hours = 24)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var history = await _historyService.GetHistoryByUserAsync(userId, currentUserId, userRole, hours);
            return Ok(history);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter histórico do usuário logado.
    [HttpGet("my-history")]
    public async Task<IActionResult> GetMyHistory([FromQuery] int hours = 24)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            Console.WriteLine($"[DEBUG] UserId extraído: {userId}, Role: {userRole}");

            var history = await _historyService.GetHistoryByUserAsync(userId, userId, userRole, hours);
            Console.WriteLine($"[DEBUG] Histórico recuperado: {history.Count} registros");
            if( history.Count == 0 )
            {
                return NotFound("Histórico não encontrado");
            }
            if (hours < 24) {
                return BadRequest("Histórico deve ter pelo menos 24 horas");
            }
            return Ok(history);
        }
        catch (AppException ex)
        {
            Console.WriteLine($"[DEBUG] ❌ AppException: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] ❌ Exception: {ex.Message}");
            return StatusCode(500, new { message = "Erro interno", error = ex.Message });
        }
    }

    // Método para obter histórico com filtros.
    [HttpPost("filter")]
    public async Task<IActionResult> GetHistoryByFilter([FromBody] HistoryFilter filter)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var history = await _historyService.GetHistoryByFilterAsync(filter, userId, userRole);
            return Ok(history);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter estatísticas por usuário.
    [HttpGet("statistics/user/{userId}")]
    public async Task<IActionResult> GetUserStatistics(int userId, [FromQuery] int hours = 24)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var stats = await _historyService.GetUserStatisticsAsync(userId, currentUserId, userRole, hours);
            return Ok(stats);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter estatísticas do usuário logado.
    [HttpGet("statistics/my-stats")]
    public async Task<IActionResult> GetMyStatistics([FromQuery] int hours = 24)
    {
        try
        {
            Console.WriteLine($"[DEBUG] GetMyStatistics chamado, hours={hours}");

            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            Console.WriteLine($"[DEBUG] UserId: {userId}, Role: {userRole}");

            var stats = await _historyService.GetUserStatisticsAsync(userId, userId, userRole, hours);
            Console.WriteLine($"[DEBUG] Estatísticas recuperadas");
            if(stats.TotalAnalyses == 0) {
                return NotFound("Estatísticas não encontradas");
            }
            if(hours < 24) {
                return BadRequest("Estatísticas devem ter pelo menos 24 horas");
            }
            return Ok(stats);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno", error = ex.Message });
        }
    }

    // Método para limpar histórico antigo (apenas admin).
    [HttpDelete("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CleanupOldHistory([FromQuery] int hours = 24)
    {
        try
        {
            await _historyService.CleanOldHistoryAsync(hours);
            return Ok(new { message = $"Histórico anterior a {hours} horas foi limpo com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para salvar emoção detectada.
    [HttpPost("cuidador-emotion")]
    [EnableRateLimiting("limite-emotion")]
    public async Task<IActionResult> SaveCuidadorEmotion([FromBody] CuidadorEmotionRequest? request)
    {
        try
        {
            if (request != null)
            {
                Console.WriteLine($"  CuidadorId: {request.CuidadorId}");
                Console.WriteLine($"  PatientName: {request.PatientName}");
                Console.WriteLine($"  DominantEmotion: {request.DominantEmotion}");
                Console.WriteLine($"  EmotionsDetected: {request.EmotionsDetected?.Count ?? 0} emoções");
                Console.WriteLine($"  Timestamp: {request.Timestamp}");
            }

            if (request == null || request.CuidadorId == 0)
            {
                return BadRequest(new { sucesso = false, message = "Request vazio ou formato inválido - verifique o JSON" });
            }

            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            if (request.CuidadorId != userId && userRole != "Admin")
            {
                return Forbid();
            }

            var emotionMappingService = HttpContext.RequestServices.GetService<IEmotionMappingService>();
            string? triggeredMessage = null;
            string? triggeredRuleId = null;

            if (!string.IsNullOrEmpty(request.DominantEmotion) && request.EmotionsDetected != null)
            {
                var percentage = request.EmotionsDetected.GetValueOrDefault(request.DominantEmotion, 0);

                if (emotionMappingService != null)
                {
                    var ruleResult = await emotionMappingService.FindMatchingRuleAsync(
                        userId,
                        request.DominantEmotion,
                        percentage
                    );
                    triggeredMessage = ruleResult.message;
                    triggeredRuleId = ruleResult.ruleId;
                }
            }

            var historyRecord = new HistoryRecord
            {
                UserId = userId,
                PatientName = request.PatientName ?? "Paciente",
                Timestamp = request.Timestamp ?? DateTime.UtcNow,
                EmotionsDetected = request.EmotionsDetected,
                DominantEmotion = request.DominantEmotion,
                DominantPercentage = request.EmotionsDetected?.GetValueOrDefault(request.DominantEmotion ?? "", 0) ?? 0,
                MessageTriggered = triggeredMessage,
                TriggeredRuleId = triggeredRuleId
            };

            await _historyService.CreateHistoryRecordAsync(historyRecord);

            return Ok(new
            {
                sucesso = true,
                message = "Emoção registrada com sucesso",
                cuidadorId = request.CuidadorId,
                dominantEmotion = request.DominantEmotion,
                suggestedMessage = triggeredMessage,
                timestamp = historyRecord.Timestamp
            });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno ao salvar emoção", error = ex.Message });
        }
    }
}

// Modelo para requisição de emoção do cuidador.
public class CuidadorEmotionRequest
{
    public int CuidadorId { get; set; }
    public string? PatientName { get; set; }
    public Dictionary<string, double>? EmotionsDetected { get; set; }
    public string? DominantEmotion { get; set; }
    public string? Age { get; set; }
    public string? Gender { get; set; }
    public DateTime? Timestamp { get; set; }
}
