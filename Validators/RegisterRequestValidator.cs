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
            .NotEmpty().WithMessage("Nome é obrigatório") // Não pode estar vazio
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres") // Não muito curto
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres"); // Não muito longo

        // Regra: Email deve ser válido
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório") // Obrigatório
            .EmailAddress().WithMessage("Email inválido") // Deve ser formato de email
            .MaximumLength(100).WithMessage("Email muito longo"); // Limite de tamanho

        // Regra: Senha deve ser forte
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória") // Obrigatória
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres") // Comprimento mínimo
            .Matches("[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula") // Uma maiúscula
            .Matches("[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula") // Uma minúscula
            .Matches("[0-9]").WithMessage("Senha deve conter pelo menos um número"); // Um número
    }
}