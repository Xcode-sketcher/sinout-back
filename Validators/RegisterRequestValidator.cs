using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

// Validador para requisições de registro
//
// Regras principais:
// - `Name`: obrigatório, apenas letras, entre 3 e 100 caracteres
// - `Email`: obrigatório, formato de e-mail, máximo 255 caracteres
// - `Password`: obrigatório, mínimo 8 caracteres, deve conter maiúscula, minúscula e número
// - `Phone`: opcional, deve seguir formato internacional simples e ter no máximo 20 caracteres
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        // Valida o nome do usuário (obrigatório, apenas letras)
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras");

        // Valida o email do usuário (obrigatório, formato válido)
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("Email muito longo");

        // Valida a senha (obrigatória, regras de complexidade)
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres")
            .Matches("[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches("[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches("[0-9]").WithMessage("Senha deve conter pelo menos um número");

        // Valida telefone (opcional, formato internacional tolerante)
        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Telefone inválido")
            .MaximumLength(20).WithMessage("Telefone muito longo")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}