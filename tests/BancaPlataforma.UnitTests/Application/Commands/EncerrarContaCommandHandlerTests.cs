using BancaPlataforma.Application.Accounts.Commands.EncerrarConta;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace BancaPlataforma.UnitTests.Application.Commands;

public sealed class EncerrarContaCommandHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private EncerrarContaCommandHandler CriarHandler() => new(
        _repositoryMock.Object,
        _unitOfWorkMock.Object);

    private static Conta CriarContaValida() => Conta.Abrir(
        Cnpj.Create("33.000.167/0001-01"),
        "Petróleo Brasileiro S.A.",
        "0001",
        "base64");

    [Fact]
    public async Task Handle_SaldoZero_DeveEncerrarConta()
    {
        var conta = CriarContaValida();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(conta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        await CriarHandler().Handle(new EncerrarContaCommand(conta.Id), CancellationToken.None);

        conta.Status.Should().Be(StatusConta.Encerrada);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComSaldo_DeveLancarDomainException()
    {
        var conta = CriarContaValida();
        conta.Depositar(100, "BRL", "Depósito", "key-dep");

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(conta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        var act = () => CriarHandler().Handle(new EncerrarContaCommand(conta.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*saldo igual a zero*");
    }

    [Fact]
    public async Task Handle_ContaNaoEncontrada_DeveLancarDomainException()
    {
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conta?)null);

        var act = () => CriarHandler().Handle(new EncerrarContaCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*não encontrada*");
    }
}