using System.Text;

namespace Fiscal.Infrastructure.Soap;

/// <summary>
/// Constrói envelopes SOAP 1.2 para os webservices NF-e conforme WSDL ENCAT.
/// Namespace xsi:schemaLocation e versão do leiaute devem ser atualizados conforme NT vigente.
/// </summary>
public static class EnvelopeSoapBuilder
{
    private const string NsEnv = "http://www.w3.org/2003/05/soap-envelope";
    private const string NsNFe = "http://www.portalfiscal.inf.br/nfe";
    private const string NsWsdl = "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4";

    public static string ConstruirEnvioLote(string xmlLoteAssinado, string versaoLayout = "4.00")
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append($"<env:Envelope xmlns:env=\"{NsEnv}\">");
        sb.Append("<env:Header/>");
        sb.Append("<env:Body>");
        sb.Append($"<nfeAutorizacaoLote xmlns=\"{NsWsdl}\">");
        sb.Append("<nfeDadosMsg>");
        sb.Append(xmlLoteAssinado);
        sb.Append("</nfeDadosMsg>");
        sb.Append("</nfeAutorizacaoLote>");
        sb.Append("</env:Body>");
        sb.Append("</env:Envelope>");
        return sb.ToString();
    }

    public static string ConstruirConsultaRecibo(string numeroRecibo, string versaoLayout = "4.00")
    {
        var xmlConsulta = $"<consReciNFe xmlns=\"{NsNFe}\" versao=\"{versaoLayout}\">" +
                          $"<tpAmb>2</tpAmb>" +
                          $"<nRec>{numeroRecibo}</nRec>" +
                          "</consReciNFe>";

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append($"<env:Envelope xmlns:env=\"{NsEnv}\">");
        sb.Append("<env:Header/>");
        sb.Append("<env:Body>");
        sb.Append($"<nfeRetAutorizacaoLote xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeRetAutorizacao4\">");
        sb.Append("<nfeDadosMsg>");
        sb.Append(xmlConsulta);
        sb.Append("</nfeDadosMsg>");
        sb.Append("</nfeRetAutorizacaoLote>");
        sb.Append("</env:Body>");
        sb.Append("</env:Envelope>");
        return sb.ToString();
    }

    public static string ConstruirStatusServico(int codigoUf, int ambiente, string versaoLayout = "4.00")
    {
        var xmlStatus = $"<consStatServ xmlns=\"{NsNFe}\" versao=\"{versaoLayout}\">" +
                        $"<tpAmb>{ambiente}</tpAmb>" +
                        $"<cUF>{codigoUf}</cUF>" +
                        "<xServ>STATUS</xServ>" +
                        "</consStatServ>";

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append($"<env:Envelope xmlns:env=\"{NsEnv}\">");
        sb.Append("<env:Header/>");
        sb.Append("<env:Body>");
        sb.Append($"<nfeStatusServico xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeStatusServico4\">");
        sb.Append("<nfeDadosMsg>");
        sb.Append(xmlStatus);
        sb.Append("</nfeDadosMsg>");
        sb.Append("</nfeStatusServico>");
        sb.Append("</env:Body>");
        sb.Append("</env:Envelope>");
        return sb.ToString();
    }
}
