using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

// Validador para requisições de login
// Regras principais:
// - `Email`: obrigatório, formato válido
// - `Password`: obrigatório
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Valida email (obrigatório, formato válido)
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");

        // Valida senha (obrigatória)
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória");
    }
}