using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

// Validador para requisições de paciente
public class PatientRequestValidator : AbstractValidator<PatientRequest>
{
    public PatientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do paciente é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100).WithMessage("Nome não pode ter mais de 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras");

        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(500).WithMessage("Informações adicionais não podem ter mais de 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.AdditionalInfo));

        // ProfilePhoto agora é int? (ID do avatar)
        RuleFor(x => x.ProfilePhoto)
            .GreaterThanOrEqualTo(0).WithMessage("ID da foto de perfil deve ser maior ou igual a 0")
            .LessThan(100).WithMessage("ID da foto de perfil inválido")
            .When(x => x.ProfilePhoto.HasValue);
    }
}
