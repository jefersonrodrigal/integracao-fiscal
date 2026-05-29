using System.Xml;
using Fiscal.Domain.Common;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Fiscal.Infrastructure.Providers;
using Microsoft.Extensions.Logging;

namespace Fiscal.Infrastructure.Soap;

public sealed class ConsultadorStatusServico(
    IClienteSoapSefaz clienteSoap,
    ProvedorSefazFactory provedorFactory,
    ILogger<ConsultadorStatusServico> logger) : IConsultadorStatusServico
{
    public async Task<Result<StatusServicoSefaz>> ConsultarAsync(
        UnidadeFederativa uf, AmbienteSefaz ambiente, CancellationToken ct = default)
    {
        if (!provedorFactory.Suporta(uf))
            return Result<StatusServicoSefaz>.Failure($"UF {uf} não suportada.");

        var config = provedorFactory.Resolver(uf).ObterConfiguracao(ambiente);
        var codigoUf = (int)uf;
        var codigoAmbiente = (int)ambiente;

        var envelope = EnvelopeSoapBuilder.ConstruirStatusServico(codigoUf, codigoAmbiente, config.VersaoLayout);

        logger.LogInformation("Consultando status SEFAZ {Uf}/{Ambiente}", uf, ambiente);

        var resultado = await clienteSoap.EnviarAsync(
            envelope, config.EndpointStatusServico, config.SoapActionStatusServico, ct);

        if (resultado.IsFailure)
            return Result<StatusServicoSefaz>.Failure(resultado.Error);

        return ParseRetornoStatus(resultado.Value!);
    }

    private static Result<StatusServicoSefaz> ParseRetornoStatus(string xmlResposta)
    {
        try
        {
            var doc = new XmlDocument();

            // Extrai body SOAP
            doc.LoadXml(xmlResposta);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
            ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

            var body = doc.SelectSingleNode("//soap:Body", ns);
            if (body is not null)
            {
                doc.LoadXml(body.InnerXml);
                ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");
            }

            var cStat = doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText ?? "0";
            var xMotivo = doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText ?? string.Empty;
            var dhRecbto = doc.SelectSingleNode("//nfe:dhRecbto", ns)?.InnerText;
            var tMed = doc.SelectSingleNode("//nfe:tMed", ns)?.InnerText;

            return Result<StatusServicoSefaz>.Success(new StatusServicoSefaz
            {
                CodigoStatus = int.TryParse(cStat, out var s) ? s : 0,
                Descricao = xMotivo,
                DataHoraRetorno = dhRecbto,
                TempoMedio = int.TryParse(tMed, out var t) ? t : null
            });
        }
        catch (Exception ex)
        {
            return Result<StatusServicoSefaz>.Failure($"Falha ao interpretar retorno do status: {ex.Message}");
        }
    }
}
