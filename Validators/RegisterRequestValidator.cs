// --- VALIDADOR DE REGISTRO: O INSPETOR DE QUALIDADE ---
// Analogia da cozinha: Como o "inspetor de qualidade" que checa se os ingredientes estão bons!
// Antes de cozinhar, verificamos se tudo está fresco e adequado.
// Este inspetor garante que os dados de registro são válidos.

using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        // Regra: Nome deve existir e ter tamanho adequado
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras");

        // Regra: Email deve ser válido
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("Email muito longo");

        // Regra: Senha deve ser forte
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres")
            .Matches("[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches("[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches("[0-9]").WithMessage("Senha deve conter pelo menos um número");

        // Regra: Telefone opcional mas deve ser válido se fornecido
        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Telefone inválido")
            .MaximumLength(20).WithMessage("Telefone muito longo")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        // Regra: Role deve ser válido
        RuleFor(x => x.Role)
            .Must(r => string.IsNullOrEmpty(r) || r == UserRole.Admin.ToString() || r == UserRole.Caregiver.ToString())
            .WithMessage($"Role inválido. Valores permitidos: {UserRole.Admin}, {UserRole.Caregiver}")
            .When(x => !string.IsNullOrEmpty(x.Role));
    }
}