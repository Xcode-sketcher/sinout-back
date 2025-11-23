using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;

namespace APISinout.Services;

/// <summary>
/// Interface para o serviço de histórico.
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// Obtém o histórico de um paciente.
    /// </summary>
    Task<List<HistoryRecordResponse>> GetHistoryByPatientAsync(string patientId, string currentUserId, string currentUserRole, int hours = 24);

    /// <summary>
    /// Obtém o histórico por filtro.
    /// </summary>
    Task<List<HistoryRecordResponse>> GetHistoryByFilterAsync(HistoryFilter filter, string currentUserId, string currentUserRole);

    /// <summary>
    /// Obtém as estatísticas de um paciente.
    /// </summary>
    Task<PatientStatistics> GetPatientStatisticsAsync(string patientId, string currentUserId, string currentUserRole, int hours = 24);

    /// <summary>
    /// Limpa o histórico antigo.
    /// </summary>
    Task CleanOldHistoryAsync(int hours = 24);

    /// <summary>
    /// Cria um registro de histórico.
    /// </summary>
    Task CreateHistoryRecordAsync(HistoryRecord record);
}

public class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _historyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPatientRepository _patientRepository;

    public HistoryService(IHistoryRepository historyRepository, IUserRepository userRepository, IPatientRepository patientRepository)
    {
        _historyRepository = historyRepository;
        _userRepository = userRepository;
        _patientRepository = patientRepository;
    }

    // Obtém o histórico de um paciente.
    public async Task<List<HistoryRecordResponse>> GetHistoryByPatientAsync(string patientId, string currentUserId, string currentUserRole, int hours = 24)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar se o paciente pertence ao usuário
        if (patient.CuidadorId != currentUserId && currentUserRole != "Admin")
            throw new AppException("Acesso negado");

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
            throw new AppException("Paciente não encontrado");

        // Verificar se o paciente pertence ao usuário
        if (patient.CuidadorId != currentUserId && currentUserRole != "Admin")
            throw new AppException("Acesso negado");

        var stats = await _historyRepository.GetPatientStatisticsAsync(patientId, hours);
        stats.PatientName = patient.Name;

        return stats;
    }

    /// <summary>
    /// Limpa o histórico antigo.
    /// </summary>
    public async Task CleanOldHistoryAsync(int hours = 24)
    {
        await _historyRepository.DeleteOldRecordsAsync(hours);
    }

    // Cria um registro de histórico.
    public async Task CreateHistoryRecordAsync(HistoryRecord record)
    {
        await _historyRepository.CreateRecordAsync(record);
    }
}
