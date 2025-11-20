using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

// Validador para requisições de mapeamento de emoções
public class EmotionMappingRequestValidator : AbstractValidator<EmotionMappingRequest>
{
    private readonly string[] _validEmotions = { "happy", "sad", "angry", "fear", "surprise", "neutral", "disgust" };
    private readonly string[] _validIntensityLevels = { "high", "moderate", "superior", "inferior" };

    public EmotionMappingRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThanOrEqualTo(0).WithMessage("ID do usuário inválido");

        RuleFor(x => x.Emotion)
            .NotEmpty().WithMessage("Emoção é obrigatória")
            .Must(e => e != null && _validEmotions.Contains(e.ToLower()))
            .WithMessage($"Emoção inválida. Valores permitidos: {string.Join(", ", _validEmotions)}");

        RuleFor(x => x.IntensityLevel)
            .NotEmpty().WithMessage("Nível de intensidade é obrigatório")
            .Must(i => i != null && _validIntensityLevels.Contains(i.ToLower()))
            .WithMessage($"Nível de intensidade inválido. Valores permitidos: {string.Join(", ", _validIntensityLevels)}");

        RuleFor(x => x.MinPercentage)
            .InclusiveBetween(0, 100).WithMessage("Percentual mínimo deve estar entre 0 e 100");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mensagem é obrigatória")
            .MinimumLength(1).WithMessage("Mensagem não pode estar vazia")
            .MaximumLength(200).WithMessage("Mensagem não pode ter mais de 200 caracteres");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 2).WithMessage("Prioridade deve ser 1 ou 2");
    }
}
