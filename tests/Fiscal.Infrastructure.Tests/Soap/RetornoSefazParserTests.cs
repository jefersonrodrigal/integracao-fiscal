using FluentAssertions;
using Fiscal.Infrastructure.Soap;

namespace Fiscal.Infrastructure.Tests.Soap;

public sealed class RetornoSefazParserTests
{
    private const string RetornoAutorizado = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
  <soap:Body>
    <retEnviNFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
      <tpAmb>2</tpAmb>
      <cUF>31</cUF>
      <dhRecbto>2024-05-01T12:00:00-03:00</dhRecbto>
      <cStat>104</cStat>
      <xMotivo>Lote processado</xMotivo>
      <protNFe versao=""4.00"">
        <infProt>
          <tpAmb>2</tpAmb>
          <verAplic>SVRS202401</verAplic>
          <chNFe>31240511222333000181550010000000011123456785</chNFe>
          <dhRecbto>2024-05-01T12:00:00-03:00</dhRecbto>
          <nProt>141240000001234</nProt>
          <digVal>AAAA</digVal>
          <cStat>100</cStat>
          <xMotivo>Autorizado o uso da NF-e</xMotivo>
        </infProt>
      </protNFe>
    </retEnviNFe>
  </soap:Body>
</soap:Envelope>";

    private const string RetornoRejeicao = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
  <soap:Body>
    <retEnviNFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
      <cStat>225</cStat>
      <xMotivo>Rejeição: Código do município do emitente inválido</xMotivo>
    </retEnviNFe>
  </soap:Body>
</soap:Envelope>";

    [Fact]
    public void ParseRetornoEnvio_deve_extrair_status_100_com_protocolo()
    {
        var resultado = RetornoSefazParser.ParseRetornoEnvio(RetornoAutorizado);

        resultado.CodigoStatus.Should().Be(104);
        resultado.Resultados.Should().HaveCount(1);
        resultado.Resultados[0].CodigoStatus.Should().Be(100);
        resultado.Resultados[0].Protocolo.Should().NotBeNull();
        resultado.Resultados[0].Protocolo!.NumeroProtocolo.Should().Be("141240000001234");
        resultado.Resultados[0].Protocolo!.ChaveAcesso.Should().Be("31240511222333000181550010000000011123456785");
    }

    [Fact]
    public void ParseRetornoEnvio_deve_reconhecer_rejeicao()
    {
        var resultado = RetornoSefazParser.ParseRetornoEnvio(RetornoRejeicao);

        resultado.CodigoStatus.Should().Be(225);
        resultado.Sucesso.Should().BeFalse();
        resultado.Resultados.Should().BeEmpty();
    }

    [Fact]
    public void ParseRetornoEnvio_deve_marcar_sucesso_quando_autorizado()
    {
        var resultado = RetornoSefazParser.ParseRetornoEnvio(RetornoAutorizado);
        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public void ParseRetornoEnvio_deve_preencher_xml_retorno_sefaz()
    {
        var resultado = RetornoSefazParser.ParseRetornoEnvio(RetornoAutorizado);
        resultado.XmlRetornoSefaz.Should().NotBeNullOrEmpty();
    }
}
