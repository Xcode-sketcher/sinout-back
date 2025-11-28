#pragma warning disable CS8600
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;
using APISinout.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;

namespace APISinout.Controllers;

// Controlador para histórico de emoções.
[ApiController]
[Route("api/history")]
[Authorize]
[EnableRateLimiting("limite-api")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;
    private readonly IPatientRepository _patientRepository;
    private readonly IEmotionMappingService _emotionMappingService;
    private readonly ILogger<HistoryController> _logger;

    // Construtor que injeta o serviço de histórico.
    public HistoryController(IHistoryService historyService, IPatientRepository patientRepository, IEmotionMappingService emotionMappingService, ILogger<HistoryController> logger)
    {
        _historyService = historyService;
        _patientRepository = patientRepository;
        _emotionMappingService = emotionMappingService;
        _logger = logger;
    }

    // Método para obter histórico por paciente.
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetHistoryByPatient(string patientId, [FromQuery] int hours = 24)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var history = await _historyService.GetHistoryByPatientAsync(patientId, currentUserId, userRole, hours);
            return Ok(history);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter histórico de todos os pacientes do cuidador logado.
    [HttpGet("my-history")]
    public async Task<IActionResult> GetMyHistory([FromQuery] int hours = 24, [FromQuery] string? patientId = null)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // Usa o filtro para pegar tudo do cuidador
            var filter = new HistoryFilter 
            { 
                CuidadorId = userId,
                StartDate = DateTime.UtcNow.AddHours(-hours)
            };

            if (!string.IsNullOrEmpty(patientId))
            {
                filter.PatientId = patientId;
            }

            var history = await _historyService.GetHistoryByFilterAsync(filter, userId, userRole);
            
            if (history.Count == 0)
            {
                return Ok(new List<HistoryRecordResponse>()); // Retorna lista vazia em vez de 404
            }
            
            return Ok(history);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao obter histórico: {Message}", ex.Message);
            return StatusCode(500, new { message = "Erro interno" });
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

    // Método para obter estatísticas por paciente.
    [HttpGet("statistics/patient/{patientId}")]
    public async Task<IActionResult> GetPatientStatistics(string patientId, [FromQuery] int hours = 24)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            var stats = await _historyService.GetPatientStatisticsAsync(patientId, currentUserId, userRole, hours);
            return Ok(stats);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // CleanupOldHistory endpoint removed - should be implemented as background job/scheduled task
    // Old endpoint was Admin-only and shouldn't be exposed via API for security

    // Método para salvar emoção detectada.
    [HttpPost("cuidador-emotion")]
    [EnableRateLimiting("limite-emotion")]
    public async Task<IActionResult> SaveCuidadorEmotion([FromBody] CuidadorEmotionRequest? request)
    {
        try
        {
            
            if (request == null)
            {
                _logger.LogWarning("CuidadorEmotion request inválido ou vazio recebido.");
                return BadRequest(new { sucesso = false, message = "Request vazio ou formato inválido" });
            }
            

            // Garantir que emotionsDetected não seja nulo ou vazio
            if (request.emotionsDetected == null || request.emotionsDetected.Count == 0)
            {
                _logger.LogDebug("CuidadorEmotion: emotionsDetected vazio. Aplicando fallback.");
                request.emotionsDetected = new Dictionary<string, double> { { request.dominantEmotion ?? "neutral", 1.0 } };
            }

            if (string.IsNullOrEmpty(request.cuidadorId))
            {
                _logger.LogWarning("CuidadorEmotion: campo cuidadorId ausente.");
                return BadRequest(new { sucesso = false, message = "Request vazio ou formato inválido" });
            }

            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            if (request.cuidadorId != userId && userRole != "Admin")
            {
                _logger.LogWarning("CuidadorEmotion: tentativa não autorizada de salvar emoção.");
                return Forbid();
            }

            // Resolver PatientId
            string patientId = string.Empty;
            string? reqPatientId = request.patientId;
            if (reqPatientId != null && MongoDB.Bson.ObjectId.TryParse(reqPatientId, out ObjectId _))
            {
                patientId = reqPatientId;
            }
            else
            {
                // Se não tem ID válido, cria um "Paciente Padrão" ou usa o primeiro paciente do usuário
                var patients = await _patientRepository.GetByCuidadorIdAsync(userId);
                var defaultPatient = patients.FirstOrDefault();
                
                if (defaultPatient != null)
                {
                    patientId = defaultPatient.Id;
                }
                else
                {
                    var newPatient = new Patient
                    {
                        Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                        Name = "Paciente Padrão",
                        CuidadorId = userId,
                        DataCadastro = DateTime.UtcNow
                    };
                    await _patientRepository.CreatePatientAsync(newPatient);
                    patientId = newPatient.Id;
                }
            }

            // Validação final do PatientId
            if (!MongoDB.Bson.ObjectId.TryParse(patientId, out _))
            {
                 patientId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            }

            // Validação do UserId
            if (!MongoDB.Bson.ObjectId.TryParse(userId, out _))
            {
                return BadRequest(new { sucesso = false, message = "ID do usuário inválido para gravação" });
            }

            string? triggeredMessage = null;
            string? triggeredRuleId = null;

            if (!string.IsNullOrEmpty(request.dominantEmotion) && request.emotionsDetected != null)
            {
                var percentage = request.emotionsDetected.GetValueOrDefault(request.dominantEmotion, 0);

                var ruleResult = await _emotionMappingService.FindMatchingRuleAsync(
                    userId,
                    request.dominantEmotion,
                    percentage
                );
                triggeredMessage = ruleResult.message;
                triggeredRuleId = ruleResult.ruleId;
                if (!string.IsNullOrEmpty(triggeredRuleId))
                {
                    _logger.LogInformation("CuidadorEmotion: regra de mapeamento acionada.");
                }
            }

            var historyRecord = new HistoryRecord
            {
                UserId = userId,
                PatientId = patientId,
                Timestamp = request.timestamp ?? DateTime.UtcNow,
                EmotionsDetected = request.emotionsDetected,
                DominantEmotion = request.dominantEmotion,
                DominantPercentage = request.emotionsDetected?.GetValueOrDefault(request.dominantEmotion ?? "", 0) ?? 0,
                MessageTriggered = triggeredMessage,
                TriggeredRuleId = triggeredRuleId
            };

            // Persistindo o registro no MongoDB

            await _historyService.CreateHistoryRecordAsync(historyRecord);
            _logger.LogInformation("CuidadorEmotion: registro salvo com sucesso. EmotionsCount={Count}", historyRecord.EmotionsDetected?.Count ?? 0);

            // Registro salvo com sucesso

            return Ok(new
            {
                sucesso = true,
                message = "Emoção registrada com sucesso",
                cuidadorId = request.cuidadorId,
                patientId = patientId,
                dominantEmotion = request.dominantEmotion,
                emotionsCount = historyRecord.EmotionsDetected?.Count ?? 0,
                suggestedMessage = triggeredMessage,
                timestamp = historyRecord.Timestamp
            });
        }
        catch (AppException ex)
        {
            // AppException ocorrida - retornando BadRequest
            _logger.LogWarning("AppException ao salvar CuidadorEmotion: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Exception crítica ocorrida - retornando 500 Internal Server Error
            _logger.LogError("Erro ao salvar CuidadorEmotion: {Message}", ex.Message);
            return StatusCode(500, new { message = "Erro interno ao salvar emoção" });
        }
    }
}

// Modelo para requisição de emoção do cuidador.
public class CuidadorEmotionRequest
{
    public string? cuidadorId { get; set; }
    public string? patientId { get; set; }
    public string? patientName { get; set; }
    public Dictionary<string, double>? emotionsDetected { get; set; }
    public string? dominantEmotion { get; set; }
    public string? age { get; set; }
    public string? gender { get; set; }
    public DateTime? timestamp { get; set; }
}
