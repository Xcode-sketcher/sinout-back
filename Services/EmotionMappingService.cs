using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;

namespace APISinout.Services;

/// <summary>
/// Interface para o serviço de mapeamento de emoções.
/// </summary>
public interface IEmotionMappingService
{
    /// <summary>
    /// Cria um novo mapeamento de emoção.
    /// </summary>
    Task<EmotionMappingResponse> CreateMappingAsync(EmotionMappingRequest request, string currentUserId, string currentUserRole);

    /// <summary>
    /// Obtém os mapeamentos de emoção de um usuário.
    /// </summary>
    Task<List<EmotionMappingResponse>> GetMappingsByUserAsync(string userId, string currentUserId, string currentUserRole);

    /// <summary>
    /// Atualiza um mapeamento de emoção.
    /// </summary>
    Task<EmotionMappingResponse> UpdateMappingAsync(string id, EmotionMappingRequest request, string currentUserId, string currentUserRole);

    /// <summary>
    /// Exclui um mapeamento de emoção.
    /// </summary>
    Task DeleteMappingAsync(string id, string currentUserId, string currentUserRole);

    /// <summary>
    /// Encontra a mensagem correspondente para uma emoção e percentual.
    /// </summary>
    Task<string?> FindMatchingMessageAsync(string userId, string emotion, double percentage);

    /// <summary>
    /// Encontra a regra correspondente (mensagem e ID) para uma emoção e percentual.
    /// </summary>
    Task<(string? message, string? ruleId)> FindMatchingRuleAsync(string userId, string emotion, double percentage);
}

public class EmotionMappingService : IEmotionMappingService
{
    private readonly IEmotionMappingRepository _mappingRepository;
    private readonly IUserRepository _userRepository;

    // Emoções válidas
    private readonly string[] _validEmotions = { "happy", "sad", "angry", "fear", "surprise", "neutral", "disgust" };
    private readonly string[] _validIntensityLevels = { "high", "moderate", "superior", "inferior" };

    public EmotionMappingService(
        IEmotionMappingRepository mappingRepository,
        IUserRepository userRepository)
    {
        _mappingRepository = mappingRepository;
        _userRepository = userRepository;
    }

    // Cria um novo mapeamento de emoção.
    public async Task<EmotionMappingResponse> CreateMappingAsync(EmotionMappingRequest request, string currentUserId, string currentUserRole)
    {
        // Validações
        if (string.IsNullOrEmpty(request.Emotion) || !_validEmotions.Contains(request.Emotion.ToLower()))
            throw new AppException($"Emoção inválida. Valores permitidos: {string.Join(", ", _validEmotions)}");

        if (string.IsNullOrEmpty(request.IntensityLevel) || !_validIntensityLevels.Contains(request.IntensityLevel.ToLower()))
            throw new AppException($"Nível de intensidade inválido. Valores permitidos: {string.Join(", ", _validIntensityLevels)}");

        if (request.MinPercentage < 0 || request.MinPercentage > 100)
            throw new AppException("Percentual mínimo deve estar entre 0 e 100");

        if (string.IsNullOrEmpty(request.Message))
            throw new AppException("Mensagem é obrigatória");

        if (request.Message.Length > 200)
            throw new AppException("Mensagem não pode ter mais de 200 caracteres");

        if (request.Priority < 1 || request.Priority > 10)
            throw new AppException("Prioridade deve estar entre 1 e 10");

        // Usar o userId do request ou o currentUserId
        var userId = !string.IsNullOrEmpty(request.UserId) ? request.UserId : currentUserId;

        // Verificar permissão
        // Usuário só pode obter mapeamentos próprios, exceto Admin
        if (currentUserRole != "Admin" && userId != currentUserId)
            throw new AppException("Acesso negado");

        // Verificar se usuário existe
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");

        // Verificar limite de 2 mapeamentos por emoção
        var count = await _mappingRepository.CountByUserAndEmotionAsync(userId, request.Emotion.ToLower());
        if (count >= 2)
            throw new AppException($"Limite de 2 mapeamentos para a emoção '{request.Emotion}' atingido.");

        // Verificar prioridade duplicada
        var existingMappings = await _mappingRepository.GetByUserAndEmotionAsync(userId, request.Emotion.ToLower());
        if (existingMappings.Any(m => m.Priority == request.Priority))
            throw new AppException($"Já existe um mapeamento com prioridade {request.Priority} para a emoção '{request.Emotion}'.");

        var mapping = new EmotionMapping
        {
            UserId = userId,
            Emotion = request.Emotion.ToLower(),
            IntensityLevel = request.IntensityLevel.ToLower(),
            MinPercentage = request.MinPercentage,
            Message = request.Message.Trim(),
            Priority = request.Priority,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _mappingRepository.CreateMappingAsync(mapping);

        return new EmotionMappingResponse(mapping, user.Name);
    }

    // Obtém os mapeamentos de emoção de um usuário.
    public async Task<List<EmotionMappingResponse>> GetMappingsByUserAsync(string userId, string currentUserId, string currentUserRole)
    {
        // Verificar permissão
        if (currentUserRole != "Admin" && userId != currentUserId)
            throw new AppException("Acesso negado");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");

        var mappings = await _mappingRepository.GetByUserIdAsync(userId);
        return mappings.Select(m => new EmotionMappingResponse(m, user.Name)).ToList();
    }

    // Atualiza um mapeamento de emoção.
    public async Task<EmotionMappingResponse> UpdateMappingAsync(string id, EmotionMappingRequest request, string currentUserId, string currentUserRole)
    {
        var mapping = await _mappingRepository.GetByIdAsync(id);
        if (mapping == null)
            throw new AppException("Mapeamento não encontrado");

        // Verificar permissão
        // Verificar se o mapeamento pertence ao usuário, exceto Admin
        if (currentUserRole != "Admin" && mapping.UserId != currentUserId)
            throw new AppException("Acesso negado");

        // Validações
        if (string.IsNullOrEmpty(request.Emotion) || !_validEmotions.Contains(request.Emotion.ToLower()))
            throw new AppException($"Emoção inválida. Valores permitidos: {string.Join(", ", _validEmotions)}");

        if (string.IsNullOrEmpty(request.IntensityLevel) || !_validIntensityLevels.Contains(request.IntensityLevel.ToLower()))
            throw new AppException($"Nível de intensidade inválido");

        if (request.MinPercentage < 0 || request.MinPercentage > 100)
            throw new AppException("Percentual mínimo deve estar entre 0 e 100");

        if (string.IsNullOrEmpty(request.Message))
            throw new AppException("Mensagem é obrigatória");

        if (request.Message.Length > 200)
            throw new AppException("Mensagem não pode ter mais de 200 caracteres");

        if (request.Priority < 1 || request.Priority > 10)
            throw new AppException("Prioridade deve estar entre 1 e 10");

        // Atualizar campos
        mapping.Emotion = request.Emotion.ToLower();
        mapping.IntensityLevel = request.IntensityLevel.ToLower();
        mapping.MinPercentage = request.MinPercentage;
        mapping.Message = request.Message.Trim();
        mapping.Priority = request.Priority;
        mapping.UpdatedAt = DateTime.UtcNow;

        await _mappingRepository.UpdateMappingAsync(id, mapping);

        string? userName = null;
        if (!string.IsNullOrEmpty(mapping.UserId))
        {
            var user = await _userRepository.GetByIdAsync(mapping.UserId);
            userName = user?.Name;
        }
        return new EmotionMappingResponse(mapping, userName);
    }

    /// <summary>
    /// Exclui um mapeamento de emoção.
    /// </summary>
    public async Task DeleteMappingAsync(string id, string currentUserId, string currentUserRole)
    {
        var mapping = await _mappingRepository.GetByIdAsync(id);
        if (mapping == null)
            throw new AppException("Mapeamento não encontrado");

        // Verificar permissão
        // Verificar se o mapeamento pertence ao usuário, exceto Admin
        if (currentUserRole != "Admin" && mapping.UserId != currentUserId)
            throw new AppException("Acesso negado");

        await _mappingRepository.DeleteMappingAsync(id);
    }

    // Encontra a mensagem correspondente para uma emoção e percentual.
    public async Task<string?> FindMatchingMessageAsync(string userId, string emotion, double percentage)
    {
        var result = await FindMatchingRuleAsync(userId, emotion, percentage);
        return result.message;
    }

    // Encontra a regra correspondente (mensagem e ID) para uma emoção e percentual.
    public async Task<(string? message, string? ruleId)> FindMatchingRuleAsync(string userId, string emotion, double percentage)
    {
        var mappings = await _mappingRepository.GetByUserAndEmotionAsync(userId, emotion.ToLower());
        
        // Filtrar por percentual mínimo E nível de intensidade
        var matchingMapping = mappings
            .Where(m => {
                bool percentageMatch = false;
                bool intensityMatch = true;
                
                // Verificar se está dentro da faixa de intensidade
                if (m.IntensityLevel == "superior" || m.IntensityLevel == "high")
                {
                    // Superior/High: >= minPercentage (acima de 50%)
                    intensityMatch = percentage >= 50;
                    percentageMatch = percentage >= m.MinPercentage;
                }
                else if (m.IntensityLevel == "inferior" || m.IntensityLevel == "moderate")
                {
                    // Inferior/Moderate: <= minPercentage (igual ou abaixo)
                    intensityMatch = percentage <= 50;
                    percentageMatch = percentage >= m.MinPercentage;
                }
                
                var matches = percentageMatch && intensityMatch;
                
                return matches;
            })
            .OrderByDescending(m => m.Priority) // Prioridade MAIOR tem precedência
            .FirstOrDefault();

        return (matchingMapping?.Message, matchingMapping?.Id);
    }
}
