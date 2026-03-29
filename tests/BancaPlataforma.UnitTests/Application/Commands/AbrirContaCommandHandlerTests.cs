using BancaPlataforma.Application.Accounts.Commands.AbrirConta;
using BancaPlataforma.Application.Common.Interfaces;
using BancaPlataforma.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BancaPlataforma.UnitTests.Application.Commands;

public sealed class AbrirContaCommandHandlerTests
{
    private readonly Mock<IContaRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IReceitaWsService> _receitaWsMock = new();

    private AbrirContaCommandHandler CriarHandler() => new(
        _repositoryMock.Object,
        _unitOfWorkMock.Object,
        _receitaWsMock.Object);

    [Fact]
    public async Task Handle_CnpjValido_DeveCriarConta()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistePorCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _receitaWsMock
            .Setup(r => r.ConsultarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DadosCnpj("Petróleo Brasileiro S.A.", "ATIVA"));

        var command = new AbrirContaCommand("33.000.167/0001-01", "0001", "base64img");
        var handler = CriarHandler();

        // Act
        var id = await handler.Handle(command, CancellationToken.None);

        // Assert
        id.Should().NotBeEmpty();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<BancaPlataforma.Domain.Aggregates.Conta>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CnpjJaExiste_DeveLancarDomainException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistePorCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AbrirContaCommand("33.000.167/0001-01", "0001", "base64img");
        var handler = CriarHandler();

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage("*já existe*");
    }

    [Fact]
    public async Task Handle_ReceitaWsIndisponivel_DeveLancarDomainException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistePorCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _receitaWsMock
            .Setup(r => r.ConsultarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DadosCnpj?)null);

        var command = new AbrirContaCommand("33.000.167/0001-01", "0001", "base64img");
        var handler = CriarHandler();

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage("*Receita Federal*");
    }

    [Fact]
    public async Task Handle_CnpjInativo_DeveLancarDomainException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistePorCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _receitaWsMock
            .Setup(r => r.ConsultarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DadosCnpj("Empresa Fechada LTDA", "BAIXADA"));

        var command = new AbrirContaCommand("33.000.167/0001-01", "0001", "base64img");
        var handler = CriarHandler();

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage("*ativo*");
    }
}