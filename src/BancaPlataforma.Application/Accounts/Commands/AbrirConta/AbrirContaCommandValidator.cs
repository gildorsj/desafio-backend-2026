using FluentValidation;

namespace BancaPlataforma.Application.Accounts.Commands.AbrirConta;

public sealed class AbrirContaCommandValidator : AbstractValidator<AbrirContaCommand>
{
    public AbrirContaCommandValidator()
    {
        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ é obrigatório.")
            .MinimumLength(14).WithMessage("CNPJ inválido.");

        RuleFor(x => x.Agencia)
            .NotEmpty().WithMessage("Agência é obrigatória.");

        RuleFor(x => x.ImagemDocumento)
            .NotEmpty().WithMessage("Imagem do documento é obrigatória.");
    }
}
