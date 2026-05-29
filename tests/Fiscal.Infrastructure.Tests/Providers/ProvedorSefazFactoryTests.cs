using FluentAssertions;
using Fiscal.Domain.Enums;
using Fiscal.Infrastructure.Providers;
using Fiscal.Infrastructure.Providers.MG;

namespace Fiscal.Infrastructure.Tests.Providers;

public sealed class ProvedorSefazFactoryTests
{
    private static ProvedorSefazFactory CriarFactory()
    {
        var provedores = new[] { new MgSefazProvider() };
        return new ProvedorSefazFactory(provedores);
    }

    [Fact]
    public void Resolver_MG_deve_retornar_MgSefazProvider()
    {
        var factory = CriarFactory();
        var provedor = factory.Resolver(UnidadeFederativa.MG);

        provedor.Should().BeOfType<MgSefazProvider>();
        provedor.Uf.Should().Be(UnidadeFederativa.MG);
    }

    [Fact]
    public void Resolver_UF_nao_suportada_deve_lancar_NotSupportedException()
    {
        var factory = CriarFactory();
        var act = () => factory.Resolver(UnidadeFederativa.SP);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*SP*não possui provedor*");
    }

    [Fact]
    public void Suporta_MG_deve_retornar_true()
    {
        var factory = CriarFactory();
        factory.Suporta(UnidadeFederativa.MG).Should().BeTrue();
    }

    [Fact]
    public void Suporta_SP_deve_retornar_false_quando_nao_registrado()
    {
        var factory = CriarFactory();
        factory.Suporta(UnidadeFederativa.SP).Should().BeFalse();
    }
}
