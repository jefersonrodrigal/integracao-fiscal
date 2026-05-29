using System.Text;
using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Fiscal.Infrastructure.Providers;
using Microsoft.Extensions.Logging;

namespace Fiscal.Infrastructure.Soap;

/// <summary>
/// Orquestra o fluxo de transmissão (envio de lote + consulta de recibo).
/// Suporta modo síncrono (lote com 1 nota) e assíncrono (polling de recibo).
/// Referência: MOC NF-e 7.0 seção 6 — Processos de negócio.
/// </summary>
public sealed class AutorizadorNFe(
    IClienteSoapSefaz clienteSoap,
    IConsultadorRecibo consultadorRecibo,
    ProvedorSefazFactory provedorFactory,
    ILogger<AutorizadorNFe> logger) : IAutorizadorNFe
{
    public async Task<Result<ResultadoTransmissao>> AutorizarAsync(NotaFiscal nota, CancellationToken ct = default)
    {
        var lote = new Lote
        {
            IdLote = GerarIdLote(),
            Ambiente = nota.Ambiente,
            Uf = nota.Uf,
            IndicadorSincronico = 1,
            DataTransmissao = DateTime.UtcNow
        };
        lote.Notas.Add(nota);

        return await AutorizarLoteAsync(lote, ct);
    }

    public async Task<Result<ResultadoTransmissao>> AutorizarLoteAsync(Lote lote, CancellationToken ct = default)
    {
        if (!provedorFactory.Suporta(lote.Uf))
            return Result<ResultadoTransmissao>.Failure($"UF {lote.Uf} não suportada.");

        var config = provedorFactory.Resolver(lote.Uf).ObterConfiguracao(lote.Ambiente);

        // 1. Montar XML do lote
        var xmlLote = MontarXmlLote(lote, config.VersaoLayout);
        lote.XmlLote = xmlLote;

        // 2. Construir envelope SOAP
        var envelope = EnvelopeSoapBuilder.ConstruirEnvioLote(xmlLote, config.VersaoLayout);

        // 3. Transmitir
        logger.LogInformation("Transmitindo lote {IdLote} para {Endpoint}", lote.IdLote, config.EndpointAutorizacao);
        var envioResult = await clienteSoap.EnviarAsync(envelope, config.EndpointAutorizacao, config.SoapActionAutorizacao, ct);

        if (envioResult.IsFailure)
            return Result<ResultadoTransmissao>.Failure(envioResult.Error);

        var resultado = RetornoSefazParser.ParseRetornoEnvio(envioResult.Value!);

        // Status 103 = lote recebido (assíncrono) — consultar recibo
        if (resultado.CodigoStatus == 103 && !string.IsNullOrEmpty(resultado.NumeroRecibo))
        {
            logger.LogInformation("Lote assíncrono. Consultando recibo {Recibo}", resultado.NumeroRecibo);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return await consultadorRecibo.ConsultarAsync(resultado.NumeroRecibo!, lote.Uf, lote.Ambiente, ct);
        }

        lote.Recibo = resultado.NumeroRecibo;

        if (resultado.Sucesso && resultado.Resultados.Count > 0 && lote.Notas.Count > 0)
        {
            var notaResult = resultado.Resultados[0];
            if (notaResult.Protocolo is not null)
            {
                var nota = lote.Notas[0];
                notaResult.Protocolo.XmlNfAutorizado = MontarNfAutorizada(nota.XmlAssinado!, notaResult.Protocolo);
                nota.XmlAutorizado = notaResult.Protocolo.XmlNfAutorizado;
            }
        }

        return Result<ResultadoTransmissao>.Success(resultado);
    }

    private static string MontarXmlLote(Lote lote, string versao)
    {
        var sb = new StringBuilder();
        sb.Append($"<enviNFe xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"{versao}\">");
        sb.Append($"<idLote>{lote.IdLote}</idLote>");
        sb.Append($"<indSinc>{lote.IndicadorSincronico}</indSinc>");
        foreach (var nota in lote.Notas)
        {
            sb.Append(nota.XmlAssinado ?? nota.XmlGerado ?? string.Empty);
        }
        sb.Append("</enviNFe>");
        return sb.ToString();
    }

    private static string MontarNfAutorizada(string xmlAssinado, ProtocoloAutorizacao protocolo)
    {
        return $"<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\">" +
               xmlAssinado +
               protocolo.XmlProtocolo +
               "</nfeProc>";
    }

    private static long GerarIdLote()
        => long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
}
