// --- VALIDADOR DE PACIENTE ---
// Validações rigorosas para criação e atualização de pacientes

using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

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

        RuleFor(x => x.ProfilePhoto)
            .MaximumLength(100000).WithMessage("Foto de perfil muito grande")
            .When(x => !string.IsNullOrEmpty(x.ProfilePhoto));
    }
}
