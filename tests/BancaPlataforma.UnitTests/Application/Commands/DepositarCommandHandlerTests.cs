using BancaPlataforma.Application.Accounts.Commands.Depositar;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace BancaPlataforma.UnitTests.Application.Commands;

public sealed class DepositarCommandHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyMock = new();

    private DepositarCommandHandler CriarHandler() => new(
        _repositoryMock.Object,
        _unitOfWorkMock.Object,
        _idempotencyMock.Object);

    private static Conta CriarContaValida() => Conta.Abrir(
        Cnpj.Create("33.000.167/0001-01"),
        "Petróleo Brasileiro S.A.",
        "0001",
        "base64");

    [Fact]
    public async Task Handle_OperacaoNova_DeveDepositarERegistrarIdempotencia()
    {
        var conta = CriarContaValida();

        _idempotencyMock
            .Setup(i => i.ExisteAsync("key-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(conta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        var command = new DepositarCommand(conta.Id, "key-001", 500, "BRL", "Depósito");
        await CriarHandler().Handle(command, CancellationToken.None);

        conta.Saldo.Valor.Should().Be(500);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _idempotencyMock.Verify(i => i.RegistrarAsync("key-001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ChaveIdempotenteDuplicada_NaoDeveDepositar()
    {
        _idempotencyMock
            .Setup(i => i.ExisteAsync("key-dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DepositarCommand(Guid.NewGuid(), "key-dup", 500, "BRL", "Depósito");
        await CriarHandler().Handle(command, CancellationToken.None);

        _repositoryMock.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ContaNaoEncontrada_DeveLancarDomainException()
    {
        _idempotencyMock
            .Setup(i => i.ExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conta?)null);

        var command = new DepositarCommand(Guid.NewGuid(), "key-001", 500, "BRL", "Depósito");
        var act = () => CriarHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*não encontrada*");
    }
}