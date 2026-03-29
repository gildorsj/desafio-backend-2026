using BancaPlataforma.Application.Accounts.Commands.Transferir;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace BancaPlataforma.UnitTests.Application.Commands;

public sealed class TransferirCommandHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyMock = new();

    private TransferirCommandHandler CriarHandler() => new(
        _repositoryMock.Object,
        _unitOfWorkMock.Object,
        _idempotencyMock.Object);

    private static Conta CriarConta(string cnpj, decimal saldo = 0)
    {
        var conta = Conta.Abrir(Cnpj.Create(cnpj), "Empresa", "0001", "base64");
        if (saldo > 0)
            conta.Depositar(saldo, "BRL", "Depósito", "key-dep-" + Guid.NewGuid());
        return conta;
    }

    [Fact]
    public async Task Handle_TransferenciaValida_DeveDebitarOrigemECreditarDestino()
    {
        var origem = CriarConta("33.000.167/0001-01", 1000);
        var destino = CriarConta("60.746.948/0001-12");

        _idempotencyMock
            .Setup(i => i.ExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(origem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(origem);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(destino.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destino);

        var command = new TransferirCommand(origem.Id, "key-trf", destino.Id, 400, "BRL", "Transferência");
        await CriarHandler().Handle(command, CancellationToken.None);

        origem.Saldo.Valor.Should().Be(600);
        destino.Saldo.Valor.Should().Be(400);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ContaOrigemNaoEncontrada_DeveLancarDomainException()
    {
        _idempotencyMock
            .Setup(i => i.ExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conta?)null);

        var command = new TransferirCommand(Guid.NewGuid(), "key-trf", Guid.NewGuid(), 100, "BRL", "Transferência");
        var act = () => CriarHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*origem*");
    }

    [Fact]
    public async Task Handle_ChaveDuplicada_NaoDeveTransferir()
    {
        _idempotencyMock
            .Setup(i => i.ExisteAsync("key-dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new TransferirCommand(Guid.NewGuid(), "key-dup", Guid.NewGuid(), 100, "BRL", "Transferência");
        await CriarHandler().Handle(command, CancellationToken.None);

        _repositoryMock.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}