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
    Task<EmotionMappingResponse> CreateMappingAsync(EmotionMappingRequest request, int currentUserId, string currentUserRole);

    /// <summary>
    /// Obtém os mapeamentos de emoção de um usuário.
    /// </summary>
    Task<List<EmotionMappingResponse>> GetMappingsByUserAsync(int userId, int currentUserId, string currentUserRole);

    /// <summary>
    /// Atualiza um mapeamento de emoção.
    /// </summary>
    Task<EmotionMappingResponse> UpdateMappingAsync(string id, EmotionMappingRequest request, int currentUserId, string currentUserRole);

    /// <summary>
    /// Exclui um mapeamento de emoção.
    /// </summary>
    Task DeleteMappingAsync(string id, int currentUserId, string currentUserRole);

    /// <summary>
    /// Encontra a mensagem correspondente para uma emoção e percentual.
    /// </summary>
    Task<string?> FindMatchingMessageAsync(int userId, string emotion, double percentage);

    /// <summary>
    /// Encontra a regra correspondente (mensagem e ID) para uma emoção e percentual.
    /// </summary>
    Task<(string? message, string? ruleId)> FindMatchingRuleAsync(int userId, string emotion, double percentage);
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
    public async Task<EmotionMappingResponse> CreateMappingAsync(EmotionMappingRequest request, int currentUserId, string currentUserRole)
    {
        Console.WriteLine($"[DEBUG] Criando mapeamento: Emotion={request.Emotion}, Intensity={request.IntensityLevel}, MinPerc={request.MinPercentage}, Priority={request.Priority}, UserId={request.UserId}, CurrentUserId={currentUserId}");

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

        if (request.Priority < 1 || request.Priority > 2)
            throw new AppException("Prioridade deve ser 1 ou 2");

        // Usar o userId do request ou o currentUserId
        var userId = request.UserId > 0 ? request.UserId : currentUserId;

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && userId != currentUserId)
            throw new AppException("Acesso negado");

        // Verificar se usuário existe
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new AppException("Usuário não encontrado");

        // Verificar limite de 2 mapeamentos por emoção
        var existingCount = await _mappingRepository.CountByUserAndEmotionAsync(userId, request.Emotion.ToLower());
        if (existingCount >= 2)
            throw new AppException($"Limite de 2 regras por emoção atingido para '{request.Emotion}'. Delete uma regra existente antes de criar uma nova.");

        // Verificar se já existe mapeamento com mesma prioridade (sensibilidade)
        var existing = await _mappingRepository.GetByUserAndEmotionAsync(userId, request.Emotion.ToLower());
        if (existing.Any(m => m.Priority == request.Priority))
            throw new AppException($"Já existe um mapeamento com prioridade {request.Priority} para a emoção '{request.Emotion}'");

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

        Console.WriteLine($"[DEBUG] Mapeamento criado com sucesso: Id={mapping.Id}");

        return new EmotionMappingResponse(mapping, user.Name);
    }

    // Obtém os mapeamentos de emoção de um usuário.
    public async Task<List<EmotionMappingResponse>> GetMappingsByUserAsync(int userId, int currentUserId, string currentUserRole)
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
    public async Task<EmotionMappingResponse> UpdateMappingAsync(string id, EmotionMappingRequest request, int currentUserId, string currentUserRole)
    {
        var mapping = await _mappingRepository.GetByIdAsync(id);
        if (mapping == null)
            throw new AppException("Mapeamento não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && mapping.UserId != currentUserId)
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

        if (request.Priority < 1 || request.Priority > 2)
            throw new AppException("Prioridade deve ser 1 ou 2");

        // Atualizar campos
        mapping.Emotion = request.Emotion.ToLower();
        mapping.IntensityLevel = request.IntensityLevel.ToLower();
        mapping.MinPercentage = request.MinPercentage;
        mapping.Message = request.Message.Trim();
        mapping.Priority = request.Priority;
        mapping.UpdatedAt = DateTime.UtcNow;

        await _mappingRepository.UpdateMappingAsync(id, mapping);

        var user = await _userRepository.GetByIdAsync(mapping.UserId);
        return new EmotionMappingResponse(mapping, user?.Name);
    }

    /// <summary>
    /// Exclui um mapeamento de emoção.
    /// </summary>
    public async Task DeleteMappingAsync(string id, int currentUserId, string currentUserRole)
    {
        var mapping = await _mappingRepository.GetByIdAsync(id);
        if (mapping == null)
            throw new AppException("Mapeamento não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && mapping.UserId != currentUserId)
            throw new AppException("Acesso negado");

        await _mappingRepository.DeleteMappingAsync(id);
    }

    // Encontra a mensagem correspondente para uma emoção e percentual.
    public async Task<string?> FindMatchingMessageAsync(int userId, string emotion, double percentage)
    {
        var result = await FindMatchingRuleAsync(userId, emotion, percentage);
        return result.message;
    }

    // Encontra a regra correspondente (mensagem e ID) para uma emoção e percentual.
    public async Task<(string? message, string? ruleId)> FindMatchingRuleAsync(int userId, string emotion, double percentage)
    {
        var mappings = await _mappingRepository.GetByUserAndEmotionAsync(userId, emotion.ToLower());
        
        Console.WriteLine($"[EmotionMapping] Buscando regra para userId={userId}, emotion={emotion}, percentage={percentage}");
        Console.WriteLine($"[EmotionMapping] Total de regras encontradas: {mappings.Count}");
        
        // Filtrar por percentual mínimo E nível de intensidade
        var matchingMapping = mappings
            .Where(m => {
                bool percentageMatch = percentage >= m.MinPercentage;
                bool intensityMatch = true;
                
                // Verificar se está dentro da faixa de intensidade
                if (m.IntensityLevel == "superior" || m.IntensityLevel == "high")
                {
                    // Superior/High: 50-100%
                    intensityMatch = percentage >= 50;
                }
                else if (m.IntensityLevel == "inferior" || m.IntensityLevel == "moderate")
                {
                    // Inferior/Moderate: 0-50%
                    intensityMatch = percentage < 50;
                }
                
                var matches = percentageMatch && intensityMatch;
                Console.WriteLine($"[EmotionMapping] Regra: id={m.Id}, level={m.IntensityLevel}, minPct={m.MinPercentage}, priority={m.Priority} => Match={matches} (pct={percentageMatch}, int={intensityMatch})");
                
                return matches;
            })
            .OrderBy(m => m.Priority)
            .FirstOrDefault();

        Console.WriteLine($"[EmotionMapping] Regra selecionada: {matchingMapping?.Message ?? "NENHUMA"} (ID: {matchingMapping?.Id ?? "null"})");
        return (matchingMapping?.Message, matchingMapping?.Id);
    }
}
