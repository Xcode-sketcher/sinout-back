using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using Microsoft.Extensions.Logging;

namespace APISinout.Services;

// Interface para o serviço de histórico
public interface IHistoryService
{
    // Obtém o histórico de um paciente.
    Task<List<HistoryRecordResponse>> GetHistoryByPatientAsync(string patientId, string currentUserId, string currentUserRole, int hours = 24);

    // Obtém o histórico por filtro.
    Task<List<HistoryRecordResponse>> GetHistoryByFilterAsync(HistoryFilter filter, string currentUserId, string currentUserRole);

    // Obtém as estatísticas de um paciente.
    Task<PatientStatistics> GetPatientStatisticsAsync(string patientId, string currentUserId, string currentUserRole, int hours = 24);

    // Limpa o histórico antigo.
    Task CleanOldHistoryAsync(int hours = 24);

    // Cria um registro de histórico.
    Task CreateHistoryRecordAsync(HistoryRecord record);
}

public class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _historyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<HistoryService>? _logger;

    public HistoryService(IHistoryRepository historyRepository, IUserRepository userRepository, IPatientRepository patientRepository, ILogger<HistoryService>? logger = null)
    {
        _historyRepository = historyRepository;
        _userRepository = userRepository;
        _patientRepository = patientRepository;
        _logger = logger;
    }

    // Obtém o histórico de um paciente.
    public async Task<List<HistoryRecordResponse>> GetHistoryByPatientAsync(string patientId, string currentUserId, string currentUserRole, int hours = 24)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            _logger?.LogWarning("HistoryService.GetHistoryByPatientAsync: paciente não encontrado. PatientId={PatientId}", patientId);
            throw new AppException("Paciente não encontrado");
        }

        // Verificar se o paciente pertence ao usuário
        if (patient.CuidadorId != currentUserId && currentUserRole != "Admin")
        {
            _logger?.LogWarning("HistoryService.GetHistoryByPatientAsync: acesso negado. PatientId={PatientId}, UserId={UserId}", patientId, currentUserId);
            throw new AppException("Acesso negado");
        }

        var records = await _historyRepository.GetByPatientIdAsync(patientId, hours);
        return records.Select(r => new HistoryRecordResponse(r, patient.Name)).ToList();
    }

    // Obtém o histórico por filtro.
    public async Task<List<HistoryRecordResponse>> GetHistoryByFilterAsync(HistoryFilter filter, string currentUserId, string currentUserRole)
    {
        // Se não for admin, só pode ver histórico próprio
        if (currentUserRole != "Admin")
        {
            filter.CuidadorId = currentUserId;
        }

        var records = await _historyRepository.GetByFilterAsync(filter);
        var responses = new List<HistoryRecordResponse>();

        foreach (var record in records)
        {
            string? patientName = null;
            if (!string.IsNullOrEmpty(record.PatientId))
            {
                 var p = await _patientRepository.GetByIdAsync(record.PatientId);
                 patientName = p?.Name;
            }
            responses.Add(new HistoryRecordResponse(record, patientName));
        }

        return responses;
    }

    // Obtém as estatísticas de um paciente.
    public async Task<PatientStatistics> GetPatientStatisticsAsync(string patientId, string currentUserId, string currentUserRole, int hours = 24)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            _logger?.LogWarning("HistoryService.GetPatientStatisticsAsync: paciente não encontrado. PatientId={PatientId}", patientId);
            throw new AppException("Paciente não encontrado");
        }

        // Verificar se o paciente pertence ao usuário
        if (patient.CuidadorId != currentUserId && currentUserRole != "Admin")
        {
            _logger?.LogWarning("HistoryService.GetPatientStatisticsAsync: acesso negado. PatientId={PatientId}, UserId={UserId}", patientId, currentUserId);
            throw new AppException("Acesso negado");
        }

        var stats = await _historyRepository.GetPatientStatisticsAsync(patientId, hours);
        stats.PatientName = patient.Name;

        return stats;
    }

    // Limpa o histórico antigo.
    public async Task CleanOldHistoryAsync(int hours = 24)
    {
        await _historyRepository.DeleteOldRecordsAsync(hours);
    }

    // Cria um registro de histórico.
    public async Task CreateHistoryRecordAsync(HistoryRecord record)
    {
        _logger?.LogDebug("HistoryService.CreateHistoryRecordAsync called. PatientId={PatientId}", record.PatientId);
        await _historyRepository.CreateRecordAsync(record);
        _logger?.LogInformation("History record created: PatientId={PatientId}", record.PatientId);
    }
}
