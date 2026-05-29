using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Fiscal.Infrastructure.Providers;
using Microsoft.Extensions.Logging;

namespace Fiscal.Infrastructure.Soap;

public sealed class ConsultadorRecibo(
    IClienteSoapSefaz clienteSoap,
    ProvedorSefazFactory provedorFactory,
    ILogger<ConsultadorRecibo> logger) : IConsultadorRecibo
{
    public async Task<Result<ResultadoTransmissao>> ConsultarAsync(
        string numeroRecibo,
        UnidadeFederativa uf,
        AmbienteSefaz ambiente,
        CancellationToken ct = default)
    {
        var config = provedorFactory.Resolver(uf).ObterConfiguracao(ambiente);
        var envelope = EnvelopeSoapBuilder.ConstruirConsultaRecibo(numeroRecibo, config.VersaoLayout);

        logger.LogInformation("Consultando recibo {Recibo} em {Endpoint}", numeroRecibo, config.EndpointRetornoAutorizacao);

        var result = await clienteSoap.EnviarAsync(
            envelope, config.EndpointRetornoAutorizacao, config.SoapActionRetornoAutorizacao, ct);

        if (result.IsFailure)
            return Result<ResultadoTransmissao>.Failure(result.Error);

        var retorno = RetornoSefazParser.ParseRetornoEnvio(result.Value!);
        return Result<ResultadoTransmissao>.Success(retorno);
    }
}
