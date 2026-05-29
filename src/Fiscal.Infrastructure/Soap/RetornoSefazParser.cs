using System.Xml;
using Fiscal.Domain.Entities;

namespace Fiscal.Infrastructure.Soap;

/// <summary>
/// Interpreta o XML de retorno da SEFAZ (retEnviNFe / retConsReciNFe).
/// Referência: Leiaute NF-e 4.00 ENCAT — elementos retEnviNFe e retConsReciNFe.
/// </summary>
public static class RetornoSefazParser
{
    public static ResultadoTransmissao ParseRetornoEnvio(string xmlRetorno)
    {
        var doc = new XmlDocument();
        doc.LoadXml(ExtrairBody(xmlRetorno));

        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

        var resultado = new ResultadoTransmissao();

        // Status do lote
        var cStat = doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText;
        var xMotivo = doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText;
        resultado.CodigoStatus = int.TryParse(cStat, out var s) ? s : 0;
        resultado.Descricao = xMotivo ?? string.Empty;
        resultado.NumeroRecibo = doc.SelectSingleNode("//nfe:nRec", ns)?.InnerText;
        resultado.XmlRetornoSefaz = xmlRetorno;

        // Protocolos individuais por nota
        var protNodes = doc.SelectNodes("//nfe:protNFe", ns);
        if (protNodes is not null)
        {
            foreach (XmlNode prot in protNodes)
            {
                var infProt = prot.SelectSingleNode("nfe:infProt", ns);
                if (infProt is null) continue;

                var resultadoNota = new ResultadoNota
                {
                    ChaveAcesso = infProt.SelectSingleNode("nfe:chNFe", ns)?.InnerText ?? string.Empty,
                    CodigoStatus = int.TryParse(infProt.SelectSingleNode("nfe:cStat", ns)?.InnerText, out var cs) ? cs : 0,
                    Descricao = infProt.SelectSingleNode("nfe:xMotivo", ns)?.InnerText ?? string.Empty
                };

                if (resultadoNota.CodigoStatus == 100)
                {
                    resultadoNota.Protocolo = new ProtocoloAutorizacao
                    {
                        ChaveAcesso = resultadoNota.ChaveAcesso,
                        NumeroProtocolo = infProt.SelectSingleNode("nfe:nProt", ns)?.InnerText ?? string.Empty,
                        CodigoStatus = resultadoNota.CodigoStatus,
                        DescricaoStatus = resultadoNota.Descricao,
                        DataHoraRecebimento = DateTime.TryParse(
                            infProt.SelectSingleNode("nfe:dhRecbto", ns)?.InnerText, out var dt) ? dt : DateTime.UtcNow,
                        DigestValue = infProt.SelectSingleNode("nfe:digVal", ns)?.InnerText,
                        XmlProtocolo = prot.OuterXml
                    };
                }

                resultado.Resultados.Add(resultadoNota);
            }
        }

        return resultado;
    }

    private static string ExtrairBody(string soapEnvelope)
    {
        // Extrai o conteúdo do Body SOAP para facilitar o parse
        var doc = new XmlDocument();
        doc.LoadXml(soapEnvelope);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
        var body = doc.SelectSingleNode("//soap:Body", ns);
        return body?.InnerXml ?? soapEnvelope;
    }
}
