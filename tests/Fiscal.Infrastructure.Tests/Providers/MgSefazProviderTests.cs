using FluentAssertions;
using Fiscal.Domain.Enums;
using Fiscal.Infrastructure.Providers.MG;

namespace Fiscal.Infrastructure.Tests.Providers;

public sealed class MgSefazProviderTests
{
    private readonly MgSefazProvider _provider = new();

    [Fact]
    public void Uf_deve_ser_MG()
    {
        _provider.Uf.Should().Be(UnidadeFederativa.MG);
    }

    [Fact]
    public void ObterConfiguracao_homologacao_deve_retornar_endpoint_hnfe()
    {
        var config = _provider.ObterConfiguracao(AmbienteSefaz.Homologacao);

        config.EndpointAutorizacao.Should().Contain("hnfe.fazenda.mg.gov.br");
        config.Ambiente.Should().Be(AmbienteSefaz.Homologacao);
    }

    [Fact]
    public void ObterConfiguracao_producao_deve_retornar_endpoint_nfe()
    {
        var config = _provider.ObterConfiguracao(AmbienteSefaz.Producao);

        config.EndpointAutorizacao.Should().Contain("nfe.fazenda.mg.gov.br");
        config.EndpointAutorizacao.Should().NotContain("hnfe");
        config.Ambiente.Should().Be(AmbienteSefaz.Producao);
    }

    [Fact]
    public void ObterConfiguracao_deve_retornar_versao_400()
    {
        var config = _provider.ObterConfiguracao(AmbienteSefaz.Homologacao);
        config.VersaoLayout.Should().Be("4.00");
    }

    [Fact]
    public void ObterRegrasValidacao_deve_retornar_regras_MG()
    {
        var regras = _provider.ObterRegrasValidacao().ToList();

        regras.Should().NotBeEmpty();
        regras.Should().Contain(r => r.Codigo == "MG-001");
        regras.Should().Contain(r => r.Codigo == "MG-002");
        regras.Should().Contain(r => r.Codigo == "MG-003");
    }

    [Fact]
    public void ObterRegrasValidacao_deve_ter_referencias_legais_preenchidas()
    {
        var regras = _provider.ObterRegrasValidacao();
        regras.Should().AllSatisfy(r => r.ReferenciaLegal.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void ObterConfiguracao_deve_ter_soap_actions_validos()
    {
        var config = _provider.ObterConfiguracao(AmbienteSefaz.Homologacao);

        config.SoapActionAutorizacao.Should().Contain("NFeAutorizacao4");
        config.SoapActionRetornoAutorizacao.Should().Contain("NFeRetAutorizacao4");
        config.SoapActionConsultaProtocolo.Should().Contain("NFeConsultaProtocolo4");
    }
}
