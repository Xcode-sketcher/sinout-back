// --- VALIDADOR DE LOGIN: O GUARDA DA PORTA ---
// Analogia da cozinha: Como o "guarda da porta" que verifica se quem entra tem permissão!
// Antes de deixar alguém entrar na cozinha, checamos se trouxe os documentos certos.
// Este validador garante que os dados de login são adequados.

using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Regra: Email deve ser válido
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório") // Não pode estar vazio
            .EmailAddress().WithMessage("Email inválido"); // Deve ser formato correto

        // Regra: Senha deve existir
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória"); // Obrigatória
    }
}