using System.Collections.Concurrent;
using APISinout.Data;
using APISinout.Models;

namespace APISinout.Tests.Integration;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId = 1;

    public Task<User?> GetByIdAsync(int id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.Values.FirstOrDefault(u => u.Email == email);
        return Task.FromResult(user);
    }

    public Task CreateUserAsync(User user)
    {
        if (user.UserId == 0)
        {
            user.UserId = _nextId++;
        }
        else if (user.UserId >= _nextId)
        {
            _nextId = user.UserId + 1;
        }
        
        _users[user.UserId] = user;
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(int id, User user)
    {
        if (_users.ContainsKey(id))
        {
            _users[id] = user;
        }
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(int id)
    {
        _users.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<List<User>> GetAllAsync()
    {
        return Task.FromResult(_users.Values.ToList());
    }

    public Task<int> GetNextUserIdAsync()
    {
        return Task.FromResult(_nextId++);
    }

    public Task UpdatePatientNameAsync(int userId, string patientName)
    {
        if (_users.TryGetValue(userId, out var user))
        {
            user.PatientName = patientName;
            user.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }
}

public class InMemoryPatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<int, Patient> _patients = new();
    private int _nextId = 1;

    public Task<Patient?> GetByIdAsync(int id)
    {
        _patients.TryGetValue(id, out var patient);
        return Task.FromResult(patient);
    }

    public Task<List<Patient>> GetByCuidadorIdAsync(int cuidadorId)
    {
        var patients = _patients.Values.Where(p => p.CuidadorId == cuidadorId && p.Status).ToList();
        return Task.FromResult(patients);
    }

    public Task<List<Patient>> GetAllAsync()
    {
        var patients = _patients.Values.Where(p => p.Status).ToList();
        return Task.FromResult(patients);
    }

    public Task CreatePatientAsync(Patient patient)
    {
        if (patient.Id == 0)
        {
            patient.Id = _nextId++;
        }
        else if (patient.Id >= _nextId)
        {
            _nextId = patient.Id + 1;
        }
        _patients[patient.Id] = patient;
        return Task.CompletedTask;
    }

    public Task UpdatePatientAsync(int id, Patient patient)
    {
        if (_patients.ContainsKey(id))
        {
            _patients[id] = patient;
        }
        return Task.CompletedTask;
    }

    public Task DeletePatientAsync(int id)
    {
        if (_patients.TryGetValue(id, out var patient))
        {
            patient.Status = false;
        }
        return Task.CompletedTask;
    }

    public Task<int> GetNextPatientIdAsync()
    {
        return Task.FromResult(_nextId++);
    }

    public Task<bool> ExistsAsync(int id)
    {
        return Task.FromResult(_patients.ContainsKey(id));
    }
}

public class InMemoryEmotionMappingRepository : IEmotionMappingRepository
{
    private readonly ConcurrentDictionary<string, EmotionMapping> _mappings = new();

    public Task<EmotionMapping?> GetByIdAsync(string id)
    {
        _mappings.TryGetValue(id, out var mapping);
        return Task.FromResult(mapping);
    }

    public Task<List<EmotionMapping>> GetByUserIdAsync(int userId)
    {
        var mappings = _mappings.Values
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Emotion)
            .ThenBy(m => m.Priority)
            .ToList();
        return Task.FromResult(mappings);
    }

    public Task<List<EmotionMapping>> GetActiveByUserIdAsync(int userId)
    {
        var mappings = _mappings.Values
            .Where(m => m.UserId == userId && m.Active)
            .OrderBy(m => m.Emotion)
            .ThenBy(m => m.Priority)
            .ToList();
        return Task.FromResult(mappings);
    }

    public Task<List<EmotionMapping>> GetByUserAndEmotionAsync(int userId, string emotion)
    {
        var mappings = _mappings.Values
            .Where(m => m.UserId == userId && m.Emotion == emotion && m.Active)
            .OrderBy(m => m.Priority)
            .ToList();
        return Task.FromResult(mappings);
    }

    public Task CreateMappingAsync(EmotionMapping mapping)
    {
        if (string.IsNullOrEmpty(mapping.Id))
        {
            mapping.Id = Guid.NewGuid().ToString();
        }
        _mappings[mapping.Id] = mapping;
        return Task.CompletedTask;
    }

    public Task UpdateMappingAsync(string id, EmotionMapping mapping)
    {
        if (_mappings.ContainsKey(id))
        {
            _mappings[id] = mapping;
        }
        return Task.CompletedTask;
    }

    public Task DeleteMappingAsync(string id)
    {
        if (_mappings.TryGetValue(id, out var mapping))
        {
            mapping.Active = false;
            mapping.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<int> CountByUserAndEmotionAsync(int userId, string emotion)
    {
        var count = _mappings.Values.Count(m => m.UserId == userId && m.Emotion == emotion && m.Active);
        return Task.FromResult(count);
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_mappings.ContainsKey(id));
    }
}

public class InMemoryHistoryRepository : IHistoryRepository
{
    private readonly ConcurrentDictionary<string, HistoryRecord> _history = new();

    public Task<HistoryRecord?> GetByIdAsync(string id)
    {
        _history.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }

    public Task<List<HistoryRecord>> GetByUserIdAsync(int userId, int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        var records = _history.Values
            .Where(h => h.UserId == userId && h.Timestamp >= cutoffTime)
            .OrderByDescending(h => h.Timestamp)
            .ToList();
        return Task.FromResult(records);
    }

    public Task<List<HistoryRecord>> GetByFilterAsync(HistoryFilter filter)
    {
        var query = _history.Values.AsQueryable();

        if (filter.PatientId.HasValue)
            query = query.Where(h => h.UserId == filter.PatientId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(h => h.Timestamp >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(h => h.Timestamp <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.DominantEmotion))
            query = query.Where(h => h.DominantEmotion == filter.DominantEmotion);

        if (filter.HasMessage.HasValue)
        {
            if (filter.HasMessage.Value)
                query = query.Where(h => h.MessageTriggered != null);
            else
                query = query.Where(h => h.MessageTriggered == null);
        }

        var records = query
            .OrderByDescending(h => h.Timestamp)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return Task.FromResult(records);
    }

    public Task CreateRecordAsync(HistoryRecord record)
    {
        if (string.IsNullOrEmpty(record.Id))
        {
            record.Id = Guid.NewGuid().ToString();
        }
        _history[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task DeleteOldRecordsAsync(int hours)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        var keysToRemove = _history.Values
            .Where(h => h.Timestamp < cutoffTime)
            .Select(h => h.Id)
            .Where(id => id != null)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _history.TryRemove(key!, out _);
        }
        return Task.CompletedTask;
    }

    public Task<PatientStatistics> GetUserStatisticsAsync(int userId, int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        var records = _history.Values
            .Where(h => h.UserId == userId && h.Timestamp >= cutoffTime)
            .ToList();

        var stats = new PatientStatistics
        {
            PatientId = userId,
            StartPeriod = cutoffTime,
            EndPeriod = DateTime.UtcNow,
            TotalAnalyses = records.Count,
            EmotionFrequency = new Dictionary<string, int>(),
            MessageFrequency = new Dictionary<string, int>(),
            EmotionTrends = new List<EmotionTrend>()
        };

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.DominantEmotion))
            {
                if (!stats.EmotionFrequency.ContainsKey(record.DominantEmotion))
                    stats.EmotionFrequency[record.DominantEmotion] = 0;
                stats.EmotionFrequency[record.DominantEmotion]++;
            }

            if (!string.IsNullOrEmpty(record.MessageTriggered))
            {
                if (!stats.MessageFrequency.ContainsKey(record.MessageTriggered))
                    stats.MessageFrequency[record.MessageTriggered] = 0;
                stats.MessageFrequency[record.MessageTriggered]++;
            }
        }

        if (stats.EmotionFrequency.Count > 0)
            stats.MostFrequentEmotion = stats.EmotionFrequency.OrderByDescending(x => x.Value).First().Key;

        if (stats.MessageFrequency.Count > 0)
            stats.MostFrequentMessage = stats.MessageFrequency.OrderByDescending(x => x.Value).First().Key;

        var groupedByHour = records
            .GroupBy(r => r.Timestamp.ToString("yyyy-MM-dd HH:00"))
            .Select(g => new EmotionTrend
            {
                Hour = g.Key,
                AnalysisCount = g.Count(),
                AverageEmotions = CalculateAverageEmotions(g.ToList())
            })
            .OrderBy(t => t.Hour)
            .ToList();

        stats.EmotionTrends = groupedByHour;

        return Task.FromResult(stats);
    }

    private Dictionary<string, double> CalculateAverageEmotions(List<HistoryRecord> records)
    {
        var emotionSums = new Dictionary<string, List<double>>();

        foreach (var record in records)
        {
            if (record.EmotionsDetected != null)
            {
                foreach (var emotion in record.EmotionsDetected)
                {
                    if (!emotionSums.ContainsKey(emotion.Key))
                        emotionSums[emotion.Key] = new List<double>();
                    emotionSums[emotion.Key].Add(emotion.Value);
                }
            }
        }

        return emotionSums.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Average()
        );
    }
}

public class InMemoryPasswordResetRepository : IPasswordResetRepository
{
    private readonly ConcurrentDictionary<string, PasswordResetToken> _tokens = new();

    public Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        var resetToken = _tokens.Values.FirstOrDefault(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.UtcNow);
        return Task.FromResult(resetToken);
    }

    public Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(int userId)
    {
        var resetToken = _tokens.Values
            .Where(t => t.UserId == userId && !t.Used && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(resetToken);
    }

    public Task CreateTokenAsync(PasswordResetToken resetToken)
    {
        if (string.IsNullOrEmpty(resetToken.Id))
        {
            resetToken.Id = Guid.NewGuid().ToString();
        }
        _tokens[resetToken.Id] = resetToken;
        return Task.CompletedTask;
    }

    public Task MarkAsUsedAsync(string id)
    {
        if (_tokens.TryGetValue(id, out var token))
        {
            token.Used = true;
            token.UsedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task DeleteExpiredTokensAsync()
    {
        var keysToRemove = _tokens.Values
            .Where(t => t.ExpiresAt < DateTime.UtcNow || t.Used)
            .Select(t => t.Id)
            .Where(id => id != null)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _tokens.TryRemove(key!, out _);
        }
        return Task.CompletedTask;
    }
}
