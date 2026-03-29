using BancaPlataforma.Application.Accounts.Commands.Sacar;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace BancaPlataforma.UnitTests.Application.Commands;

public sealed class SacarCommandHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyMock = new();

    private SacarCommandHandler CriarHandler() => new(
        _repositoryMock.Object,
        _unitOfWorkMock.Object,
        _idempotencyMock.Object);

    private static Conta CriarContaComSaldo(decimal saldo)
    {
        var conta = Conta.Abrir(
            Cnpj.Create("33.000.167/0001-01"),
            "Petróleo Brasileiro S.A.",
            "0001",
            "base64");
        if (saldo > 0)
            conta.Depositar(saldo, "BRL", "Depósito inicial", "key-dep");
        return conta;
    }

    [Fact]
    public async Task Handle_SaldoSuficiente_DeveSacarERegistrarIdempotencia()
    {
        var conta = CriarContaComSaldo(1000);

        _idempotencyMock
            .Setup(i => i.ExisteAsync("key-saq", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(conta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        var command = new SacarCommand(conta.Id, "key-saq", 300, "BRL", "Saque");
        await CriarHandler().Handle(command, CancellationToken.None);

        conta.Saldo.Valor.Should().Be(700);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _idempotencyMock.Verify(i => i.RegistrarAsync("key-saq", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaldoInsuficiente_DeveLancarDomainException()
    {
        var conta = CriarContaComSaldo(100);

        _idempotencyMock
            .Setup(i => i.ExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(conta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        var command = new SacarCommand(conta.Id, "key-saq", 500, "BRL", "Saque");
        var act = () => CriarHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Saldo insuficiente*");
    }

    [Fact]
    public async Task Handle_ChaveDuplicada_NaoDeveSacar()
    {
        _idempotencyMock
            .Setup(i => i.ExisteAsync("key-dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new SacarCommand(Guid.NewGuid(), "key-dup", 100, "BRL", "Saque");
        await CriarHandler().Handle(command, CancellationToken.None);

        _repositoryMock.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}