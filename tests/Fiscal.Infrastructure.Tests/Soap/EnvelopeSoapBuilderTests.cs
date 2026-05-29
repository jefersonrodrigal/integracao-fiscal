using System.Xml;
using FluentAssertions;
using Fiscal.Infrastructure.Soap;

namespace Fiscal.Infrastructure.Tests.Soap;

public sealed class EnvelopeSoapBuilderTests
{
    [Fact]
    public void ConstruirEnvioLote_deve_gerar_envelope_soap_valido()
    {
        var envelope = EnvelopeSoapBuilder.ConstruirEnvioLote("<enviNFe/>", "4.00");

        envelope.Should().NotBeNullOrEmpty();
        envelope.Should().Contain("Envelope");
        envelope.Should().Contain("Body");
        envelope.Should().Contain("nfeAutorizacaoLote");
        envelope.Should().Contain("nfeDadosMsg");
    }

    [Fact]
    public void ConstruirEnvioLote_deve_ser_xml_parseavel()
    {
        var envelope = EnvelopeSoapBuilder.ConstruirEnvioLote("<enviNFe/>", "4.00");

        var doc = new XmlDocument();
        var act = () => doc.LoadXml(envelope);
        act.Should().NotThrow();
    }

    [Fact]
    public void ConstruirConsultaRecibo_deve_conter_numero_recibo()
    {
        var envelope = EnvelopeSoapBuilder.ConstruirConsultaRecibo("141240000001234", "4.00");

        envelope.Should().Contain("141240000001234");
        envelope.Should().Contain("nRec");
        envelope.Should().Contain("consReciNFe");
    }

    [Fact]
    public void ConstruirStatusServico_deve_conter_codigo_uf()
    {
        var envelope = EnvelopeSoapBuilder.ConstruirStatusServico(31, 2, "4.00");

        envelope.Should().Contain("<cUF>31</cUF>");
        envelope.Should().Contain("STATUS");
        envelope.Should().Contain("consStatServ");
    }

    [Fact]
    public void ConstruirEnvioLote_deve_embutir_xml_lote_passado()
    {
        var xmlLote = "<enviNFe versao=\"4.00\"><NFe/></enviNFe>";
        var envelope = EnvelopeSoapBuilder.ConstruirEnvioLote(xmlLote);

        envelope.Should().Contain(xmlLote);
    }
}
