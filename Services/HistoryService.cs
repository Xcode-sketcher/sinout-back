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
    /// Obtém o histórico de um usuário.
    /// </summary>
    Task<List<HistoryRecordResponse>> GetHistoryByUserAsync(int userId, int currentUserId, string currentUserRole, int hours = 24);

    /// <summary>
    /// Obtém o histórico por filtro.
    /// </summary>
    Task<List<HistoryRecordResponse>> GetHistoryByFilterAsync(HistoryFilter filter, int currentUserId, string currentUserRole);

    /// <summary>
    /// Obtém as estatísticas de um usuário.
    /// </summary>
    Task<PatientStatistics> GetUserStatisticsAsync(int userId, int currentUserId, string currentUserRole, int hours = 24);

    /// <summary>
    /// Limpa o histórico antigo.
    /// </summary>
    Task CleanOldHistoryAsync(int hours = 24);

    /// <summary>
    /// Cria um registro de histórico.
    /// </summary>
    Task CreateHistoryRecordAsync(HistoryRecord record);
}

/// <summary>
/// Implementação do serviço de histórico.
/// </summary>
public class HistoryService : IHistoryService
{
    Task<List<HistoryRecordResponse>> GetHistoryByUserAsync(int userId, int currentUserId, string currentUserRole, int hours = 24);
    Task<List<HistoryRecordResponse>> GetHistoryByFilterAsync(HistoryFilter filter, int currentUserId, string currentUserRole);
    Task<PatientStatistics> GetUserStatisticsAsync(int userId, int currentUserId, string currentUserRole, int hours = 24);
    Task CleanOldHistoryAsync(int hours = 24);
    Task CreateHistoryRecordAsync(HistoryRecord record);
}

public class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _historyRepository;
    private readonly IUserRepository _userRepository;

    public HistoryService(IHistoryRepository historyRepository, IUserRepository userRepository)
    {
        _historyRepository = historyRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Obtém o histórico de um usuário.
    /// </summary>
    public async Task<List<HistoryRecordResponse>> GetHistoryByUserAsync(int userId, int currentUserId, string currentUserRole, int hours = 24)
    {
        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && userId != currentUserId)
            throw new AppException("Acesso negado");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");

        var records = await _historyRepository.GetByUserIdAsync(userId, hours);
        return records.Select(r => new HistoryRecordResponse(r, user.Name)).ToList();
    }

    /// <summary>
    /// Obtém o histórico por filtro.
    /// </summary>
    public async Task<List<HistoryRecordResponse>> GetHistoryByFilterAsync(HistoryFilter filter, int currentUserId, string currentUserRole)
    {
        // Se não for Admin, só pode ver histórico próprio
        if (currentUserRole != UserRole.Admin.ToString())
        {
            filter.PatientId = currentUserId;
        }

        var records = await _historyRepository.GetByFilterAsync(filter);
        var responses = new List<HistoryRecordResponse>();

        foreach (var record in records)
        {
            var user = await _userRepository.GetByIdAsync(record.UserId);
            responses.Add(new HistoryRecordResponse(record, user?.Name));
        }

        return responses;
    }

    /// <summary>
    /// Obtém as estatísticas de um usuário.
    /// </summary>
    public async Task<PatientStatistics> GetUserStatisticsAsync(int userId, int currentUserId, string currentUserRole, int hours = 24)
    {
        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && userId != currentUserId)
            throw new AppException("Acesso negado");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");

        var stats = await _historyRepository.GetUserStatisticsAsync(userId, hours);
        stats.PatientName = user.Name;

        return stats;
    }

    /// <summary>
    /// Limpa o histórico antigo.
    /// </summary>
    public async Task CleanOldHistoryAsync(int hours = 24)
    {
        await _historyRepository.DeleteOldRecordsAsync(hours);
    }

    /// <summary>
    /// Cria um registro de histórico.
    /// </summary>
    public async Task CreateHistoryRecordAsync(HistoryRecord record)
    {
        Console.WriteLine($"[DEBUG SERVICE] CreateHistoryRecordAsync chamado - UserId: {record.UserId}");
        await _historyRepository.CreateRecordAsync(record);
        Console.WriteLine($"[DEBUG SERVICE] CreateHistoryRecordAsync concluído");
    }
}
