using FluentValidation;
using APISinout.Models;

namespace APISinout.Validators;

// Validador para requisições de esquecimento de senha
// Regras: `Email` é obrigatório e deve ser um endereço de e-mail válido
public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        // Valida o email usado para recuperação (obrigatório)
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");
    }
}

// Validador para requisições de redefinição de senha
// Regras principais:
// - `Token`: obrigatório
// - `NewPassword`: obrigatório, comprimento mínimo e complexidade
// - `ConfirmPassword`: deve coincidir com `NewPassword`
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        // Valida token (obrigatório)
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório");

        // Valida nova senha (obrigatória, regras de complexidade)
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres")
            .MaximumLength(100).WithMessage("Senha não pode ter mais de 100 caracteres")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches(@"[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches(@"[0-9]").WithMessage("Senha deve conter pelo menos um número");

        // Valida confirmação da nova senha (deve ser igual à nova senha)
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
            .Equal(x => x.NewPassword).WithMessage("Senhas não coincidem");
    }
}

// Validador para requisições de alteração de senha
// Regras principais:
// - `CurrentPassword`: obrigatório
// - `NewPassword`/`ConfirmPassword`: mesmas regras do reset
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        // Valida senha atual (obrigatória)
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Senha atual é obrigatória");

        // Valida nova senha (obrigatória, regras de complexidade)
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres")
            .MaximumLength(100).WithMessage("Senha não pode ter mais de 100 caracteres")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches(@"[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches(@"[0-9]").WithMessage("Senha deve conter pelo menos um número");

        // Valida confirmação da nova senha (deve ser igual à nova senha)
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
            .Equal(x => x.NewPassword).WithMessage("Senhas não coincidem");
    }
}
