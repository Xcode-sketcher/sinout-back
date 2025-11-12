// --- SERVIÇO DE HISTÓRICO E ESTATÍSTICAS ---
// Gerencia histórico de análises e gera estatísticas para o dashboard

using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;

namespace APISinout.Services;

public interface IHistoryService
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

    public async Task CleanOldHistoryAsync(int hours = 24)
    {
        await _historyRepository.DeleteOldRecordsAsync(hours);
    }

    public async Task CreateHistoryRecordAsync(HistoryRecord record)
    {
        Console.WriteLine($"[DEBUG SERVICE] CreateHistoryRecordAsync chamado - UserId: {record.UserId}");
        await _historyRepository.CreateRecordAsync(record);
        Console.WriteLine($"[DEBUG SERVICE] CreateHistoryRecordAsync concluído");
    }
}
