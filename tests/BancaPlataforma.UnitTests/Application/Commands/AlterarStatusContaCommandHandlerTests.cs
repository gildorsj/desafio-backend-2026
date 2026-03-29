using BancaPlataforma.Application.Accounts.Commands.AlterarStatusConta;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Aggregates;
using BancaPlataforma.Domain.Enums;
using BancaPlataforma.Domain.Exceptions;
using BancaPlataforma.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace BancaPlataforma.UnitTests.Application.Commands;

public sealed class AlterarStatusContaCommandHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private AlterarStatusContaCommandHandler CriarHandler() => new(
        _repositoryMock.Object,
        _unitOfWorkMock.Object);

    private static Conta CriarContaValida() => Conta.Abrir(
        Cnpj.Create("33.000.167/0001-01"),
        "Petróleo Brasileiro S.A.",
        "0001",
        "base64");

    [Theory]
    [InlineData("Bloqueada", StatusConta.Bloqueada)]
    [InlineData("Ativa", StatusConta.Ativa)]
    public async Task Handle_StatusValido_DeveAlterarStatus(string novoStatus, StatusConta esperado)
    {
        var conta = CriarContaValida();
        if (esperado == StatusConta.Ativa)
            conta.AlterarStatus(StatusConta.Bloqueada); // precisa estar bloqueada para voltar a Ativa

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(conta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        var command = new AlterarStatusContaCommand(conta.Id, novoStatus);
        await CriarHandler().Handle(command, CancellationToken.None);

        conta.Status.Should().Be(esperado);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StatusInvalido_DeveLancarDomainException()
    {
        var conta = CriarContaValida();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(conta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        var command = new AlterarStatusContaCommand(conta.Id, "StatusInexistente");
        var act = () => CriarHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*inválido*");
    }

    [Fact]
    public async Task Handle_ContaNaoEncontrada_DeveLancarDomainException()
    {
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conta?)null);

        var command = new AlterarStatusContaCommand(Guid.NewGuid(), "Bloqueada");
        var act = () => CriarHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*não encontrada*");
    }
}