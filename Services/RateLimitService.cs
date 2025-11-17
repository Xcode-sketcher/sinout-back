// --- SERVIÇO DE RATE LIMITING ---
// Previne spam de emails controlando tentativas por email

using System.Collections.Concurrent;

namespace APISinout.Services;

public interface IRateLimitService
{
    bool IsRateLimited(string key, int maxAttempts = 3, int windowMinutes = 15);
    void RecordAttempt(string key);
    void ClearAttempts(string key);
}

public class RateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _attempts = new();
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(ILogger<RateLimitService> logger)
    {
        _logger = logger;
    }

    public bool IsRateLimited(string key, int maxAttempts = 3, int windowMinutes = 15)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-windowMinutes);

        if (!_attempts.TryGetValue(key, out var attemptList))
        {
            return false;
        }

        // Remove tentativas fora da janela de tempo
        var recentAttempts = attemptList.Where(t => t >= windowStart).ToList();
        _attempts[key] = recentAttempts;

        var isLimited = recentAttempts.Count >= maxAttempts;
        
        if (isLimited)
        {
            var oldestAttempt = recentAttempts.Min();
            var timeUntilReset = oldestAttempt.AddMinutes(windowMinutes) - now;
            _logger.LogWarning($"[RateLimit] Bloqueado: {key} - {recentAttempts.Count}/{maxAttempts} tentativas. Retry em {timeUntilReset.TotalMinutes:F1} minutos");
        }

        return isLimited;
    }

    public void RecordAttempt(string key)
    {
        _attempts.AddOrUpdate(
            key,
            new List<DateTime> { DateTime.UtcNow },
            (_, list) =>
            {
                list.Add(DateTime.UtcNow);
                return list;
            });

        _logger.LogInformation($"[RateLimit] Tentativa registrada para: {key}");
    }

    public void ClearAttempts(string key)
    {
        _attempts.TryRemove(key, out _);
        _logger.LogInformation($"[RateLimit] Tentativas limpas para: {key}");
    }
}
