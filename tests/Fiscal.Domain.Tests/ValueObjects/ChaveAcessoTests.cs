using FluentAssertions;
using Fiscal.Domain.ValueObjects;

namespace Fiscal.Domain.Tests.ValueObjects;

public sealed class ChaveAcessoTests
{
    [Fact]
    public void Criar_deve_gerar_chave_44_digitos()
    {
        var chave = ChaveAcesso.Criar(
            cUF: 31, anoMes: 2405, cnpj: "11222333000181",
            modelo: 55, serie: 1, numero: 1, tipoEmissao: 1, codigoNumerico: 12345678);

        chave.Valor.Should().HaveLength(44);
        chave.Valor.Should().MatchRegex(@"^\d{44}$");
    }

    [Fact]
    public void Criar_deve_comecar_com_cUF_MG()
    {
        var chave = ChaveAcesso.Criar(31, 2405, "11222333000181", 55, 1, 1, 1, 12345678);
        chave.Valor.Should().StartWith("31");
    }

    [Fact]
    public void FromString_deve_aceitar_chave_valida_44_digitos()
    {
        var raw = "31240511222333000181550010000000011123456785";
        var chave = ChaveAcesso.FromString(raw);
        chave.Valor.Should().Be(raw);
    }

    [Fact]
    public void FromString_deve_falhar_com_chave_invalida()
    {
        var act = () => ChaveAcesso.FromString("123");
        act.Should().Throw<ArgumentException>();
    }
}
