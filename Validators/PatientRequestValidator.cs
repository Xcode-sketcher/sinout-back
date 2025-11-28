using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

// Validador para requisições de paciente
//
// Regras principais:
// - `Name`: obrigatório, apenas letras, entre 3 e 100 caracteres
// - `AdditionalInfo`: opcional, máximo 500 caracteres
// - `ProfilePhoto`: opcional (ID), deve estar dentro do intervalo permitido
public class PatientRequestValidator : AbstractValidator<PatientRequest>
{
    public PatientRequestValidator()
    {
        // Valida o nome do paciente (obrigatório, apenas letras)
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do paciente é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100).WithMessage("Nome não pode ter mais de 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras");

        // Valida informações adicionais do paciente (opcional)
        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(500).WithMessage("Informações adicionais não podem ter mais de 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.AdditionalInfo));

        // ProfilePhoto agora é int? (ID do avatar)
        // Valida o ID da foto de perfil (opcional)
        RuleFor(x => x.ProfilePhoto)
            .GreaterThanOrEqualTo(0).WithMessage("ID da foto de perfil deve ser maior ou igual a 0")
            .LessThan(100).WithMessage("ID da foto de perfil inválido")
            .When(x => x.ProfilePhoto.HasValue);
    }
}
