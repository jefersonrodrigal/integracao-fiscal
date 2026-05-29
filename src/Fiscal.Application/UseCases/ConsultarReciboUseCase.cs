using Fiscal.Application.DTOs;
using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fiscal.Application.UseCases;

public sealed class ConsultarReciboUseCase(
    IConsultadorRecibo consultadorRecibo,
    IRepositorioProtocolo repositorioProtocolo,
    ILogger<ConsultarReciboUseCase> logger)
{
    public async Task<Result<ResultadoTransmissao>> ExecutarAsync(
        string numeroRecibo,
        UnidadeFederativa uf,
        AmbienteSefaz ambiente,
        CancellationToken ct = default)
    {
        logger.LogInformation("Consultando recibo {Recibo} UF={Uf}", numeroRecibo, uf);

        var resultado = await consultadorRecibo.ConsultarAsync(numeroRecibo, uf, ambiente, ct);

        if (resultado.IsFailure)
        {
            logger.LogWarning("Falha ao consultar recibo: {Erro}", resultado.Error);
            return resultado;
        }

        var retorno = resultado.Value!;

        if (retorno.Sucesso)
        {
            foreach (var nota in retorno.Resultados.Where(r => r.Protocolo is not null))
            {
                var saveResult = await repositorioProtocolo.SalvarAsync(nota.Protocolo!, ct);
                if (saveResult.IsFailure)
                    logger.LogWarning("Falha ao persistir protocolo da consulta: {Erro}", saveResult.Error);
            }
        }

        return resultado;
    }
}
