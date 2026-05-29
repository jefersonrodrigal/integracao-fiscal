using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fiscal.Infrastructure.Danfe;

/// <summary>
/// Implementação placeholder para geração de DANFE/DANFCE.
/// Em produção, substitua por biblioteca como NFe.Core, FastReport, iTextSharp,
/// QuestPDF, ou serviço especializado, respeitando o leiaute oficial ENCAT.
///
/// Referências obrigatórias para implementação real:
/// - Manual de Orientação do Contribuinte (MOC) NF-e 7.0, Capítulo 8 (DANFE)
/// - NT 2013.006 (DANFCE)
/// - Portaria CAT 162/2008 SP, RICMS estaduais vigentes
/// - Resolução SEFAZ sobre impressão em PDF
/// </summary>
public sealed class GeradorDanfePlaceholder(ILogger<GeradorDanfePlaceholder> logger) : IGeradorDanfe
{
    public Task<Result<byte[]>> GerarAsync(NotaFiscal nota, TipoDocumentoFiscal tipo, CancellationToken ct = default)
    {
        logger.LogWarning(
            "GeradorDanfePlaceholder: DANFE não implementado. Substitua por biblioteca de PDF em produção. Tipo={Tipo}",
            tipo);

        // Retorna bytes simulados — substitua pela geração real
        var pdfBytes = System.Text.Encoding.UTF8.GetBytes($"[DANFE PLACEHOLDER - {tipo} - {nota.ChaveAcesso?.Valor}]");
        return Task.FromResult(Result<byte[]>.Success(pdfBytes));
    }

    public Task<Result<byte[]>> GerarDeXmlAsync(string xmlAutorizado, TipoDocumentoFiscal tipo, CancellationToken ct = default)
    {
        logger.LogWarning("GeradorDanfePlaceholder: GerarDeXml não implementado. Tipo={Tipo}", tipo);
        var pdfBytes = System.Text.Encoding.UTF8.GetBytes($"[DANFE PLACEHOLDER XML - {tipo}]");
        return Task.FromResult(Result<byte[]>.Success(pdfBytes));
    }
}
