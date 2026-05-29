using FluentAssertions;
using Fiscal.Domain.Common;

namespace Fiscal.Domain.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_deve_ter_IsSuccess_verdadeiro()
    {
        var result = Result<string>.Success("ok");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void Failure_deve_ter_IsFailure_verdadeiro()
    {
        var result = Result<string>.Failure("erro");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("erro");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Failure_com_lista_deve_popular_Errors()
    {
        var erros = new[] { "erro1", "erro2" };
        var result = Result<string>.Failure(erros);
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("erro1");
    }

    [Fact]
    public void Match_deve_executar_onSuccess_quando_sucesso()
    {
        var result = Result<int>.Success(42);
        var saida = result.Match(v => v * 2, _ => 0);
        saida.Should().Be(84);
    }

    [Fact]
    public void Match_deve_executar_onFailure_quando_falha()
    {
        var result = Result<int>.Failure("falhou");
        var saida = result.Match(_ => 1, e => -1);
        saida.Should().Be(-1);
    }
}
