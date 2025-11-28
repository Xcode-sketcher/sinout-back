using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

// Validador para requisições de mapeamento de emoções
// Regras principais:
// - `UserId`: opcional, se informado deve ser um ObjectId do MongoDB
// - `Emotion`: obrigatório, deve ser um dos valores permitidos
// - `IntensityLevel`: obrigatório, deve ser um dos níveis permitidos
// - `MinPercentage`: obrigatório, entre 0 e 100
// - `Message`: obrigatório, entre 1 e 200 caracteres
// - `Priority`: obrigatório, deve ser 1 ou 2
public class EmotionMappingRequestValidator : AbstractValidator<EmotionMappingRequest>
{
    // Emoções aceitas pelo sistema
    private readonly string[] _validEmotions = { "happy", "sad", "angry", "fear", "surprise", "neutral", "disgust" };
    // Níveis de intensidade aceitos
    private readonly string[] _validIntensityLevels = { "high", "moderate", "superior", "inferior" };

    public EmotionMappingRequestValidator()
    {
        // UserId pode ser vazio (usa o usuário autenticado) ou um ObjectId válido
        RuleFor(x => x.UserId)
            .Must(id => string.IsNullOrEmpty(id) || MongoDB.Bson.ObjectId.TryParse(id, out _))
            .WithMessage("ID do usuário inválido");

        // Validação da emoção informada (obrigatório e deve pertencer às emoções válidas)
        RuleFor(x => x.Emotion)
            .NotEmpty().WithMessage("Emoção é obrigatória")
            .Must(e => e != null && _validEmotions.Contains(e.ToLower()))
            .WithMessage($"Emoção inválida. Valores permitidos: {string.Join(", ", _validEmotions)}");

        // Validação do nível de intensidade (obrigatório e deve pertencer aos níveis válidos)
        RuleFor(x => x.IntensityLevel)
            .NotEmpty().WithMessage("Nível de intensidade é obrigatório")
            .Must(i => i != null && _validIntensityLevels.Contains(i.ToLower()))
            .WithMessage($"Nível de intensidade inválido. Valores permitidos: {string.Join(", ", _validIntensityLevels)}");

        // Percentual mínimo que ativa o mapeamento (0 a 100)
        RuleFor(x => x.MinPercentage)
            .InclusiveBetween(0, 100).WithMessage("Percentual mínimo deve estar entre 0 e 100");

        // Mensagem que será exibida ao detectar a emoção (obrigatória, 1-200 chars)
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mensagem é obrigatória")
            .MinimumLength(1).WithMessage("Mensagem não pode estar vazia")
            .MaximumLength(200).WithMessage("Mensagem não pode ter mais de 200 caracteres");

        // Prioridade do mapeamento (1 ou 2)
        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 2).WithMessage("Prioridade deve ser 1 ou 2");
    }
}
