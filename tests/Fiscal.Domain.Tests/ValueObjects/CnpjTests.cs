using FluentAssertions;
using Fiscal.Domain.ValueObjects;

namespace Fiscal.Domain.Tests.ValueObjects;

public sealed class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Construtor_deve_aceitar_cnpj_valido(string cnpj)
    {
        var act = () => new Cnpj(cnpj);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("00.000.000/0000-00")]
    [InlineData("11111111111111")]
    [InlineData("123")]
    public void Construtor_deve_rejeitar_cnpj_invalido(string cnpj)
    {
        var act = () => new Cnpj(cnpj);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Formatado_deve_retornar_mascara_correta()
    {
        var cnpj = new Cnpj("11222333000181");
        cnpj.Formatado.Should().Be("11.222.333/0001-81");
    }
}
