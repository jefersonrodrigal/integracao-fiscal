using Fiscal.Application.DTOs;
using Fiscal.Application.UseCases;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fiscal.Api.Controllers;

/// <summary>
/// Operações de emissão e consulta de NF-e / NFC-e junto à SEFAZ.
/// </summary>
[ApiController]
[Route("api/v1/nfe")]
[Produces("application/json")]
[Tags("NF-e / NFC-e")]
public sealed class NFeController(
    EmitirNFeUseCase emitirNFeUseCase,
    ConsultarReciboUseCase consultarReciboUseCase,
    ConsultarStatusServicoUseCase consultarStatusServicoUseCase) : ControllerBase
{
    /// <summary>Emite uma NF-e ou NFC-e completa.</summary>
    /// <remarks>
    /// Executa o fluxo completo de autorização:
    ///
    /// 1. Cria a entidade a partir dos dados informados
    /// 2. Gera o XML conforme leiaute 4.00 (ENCAT)
    /// 3. Valida contra o XSD oficial da SEFAZ
    /// 4. Assina digitalmente com certificado A1 (ICP-Brasil)
    /// 5. Transmite o lote para a SEFAZ via SOAP
    /// 6. Retorna o protocolo de autorização (nProt)
    /// 7. Salva protocolo e XML autorizado em disco
    /// 8. Gera DANFE / DANFCE em PDF
    ///
    /// Exemplo mínimo para NF-e em homologação (MG):
    ///
    ///     {
    ///       "modelo": "NFe",
    ///       "ambiente": "Homologacao",
    ///       "uf": "MG",
    ///       "naturezaOperacao": "Venda de mercadoria",
    ///       "serie": 1,
    ///       "numero": 1,
    ///       "emitente": {
    ///         "cnpj": "11222333000181",
    ///         "razaoSocial": "Empresa Teste Ltda",
    ///         "inscricaoEstadual": "0629328440072",
    ///         "cnaeCode": "4711301",
    ///         "codigoRegimeTributario": "3",
    ///         "endereco": {
    ///           "logradouro": "Av. do Contorno",
    ///           "numero": "1000",
    ///           "bairro": "Funcionarios",
    ///           "codigoMunicipio": "3106200",
    ///           "nomeMunicipio": "Belo Horizonte",
    ///           "uf": "MG",
    ///           "cep": "30110090"
    ///         }
    ///       },
    ///       "produtos": [
    ///         {
    ///           "codigo": "001",
    ///           "descricao": "Notebook Intel i7",
    ///           "ncm": "84713012",
    ///           "cfop": "5102",
    ///           "unidade": "UN",
    ///           "quantidade": 1,
    ///           "valorUnitario": 3500.00,
    ///           "imposto": {
    ///             "origem": "0",
    ///             "cstCsosn": "00",
    ///             "baseCalculoIcms": 3500.00,
    ///             "aliquotaIcms": 12.0,
    ///             "cstPis": "01",
    ///             "cstCofins": "01"
    ///           }
    ///         }
    ///       ]
    ///     }
    /// </remarks>
    /// <param name="request">Dados completos da nota fiscal (emitente, destinatário, produtos, impostos).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Nota autorizada — retorna chave de acesso, número de protocolo e DANFE em base64.</response>
    /// <response code="400">Nota rejeitada pela SEFAZ — retorna código e motivo da rejeição.</response>
    /// <response code="422">Dados inválidos antes de chegar à SEFAZ (XSD, assinatura, CNPJ etc.).</response>
    [HttpPost("emitir")]
    [ProducesResponseType<EmitirNFeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<EmitirNFeResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Emitir([FromBody] EmitirNFeRequest request, CancellationToken ct)
    {
        var result = await emitirNFeUseCase.ExecutarAsync(request, ct);

        if (result.IsFailure)
            return UnprocessableEntity(new { erros = result.Errors });

        var response = result.Value!;
        if (!response.Sucesso)
            return BadRequest(response);

        return Ok(response);
    }

    /// <summary>Consulta o status de operação do webservice da SEFAZ.</summary>
    /// <remarks>
    /// Retorna se o serviço está online (cStat 107) ou em manutenção.
    ///
    /// Exemplo:
    ///
    ///     GET /api/v1/nfe/status-servico?uf=MG&amp;ambiente=Homologacao
    /// </remarks>
    /// <param name="uf">UF emissora (ex: MG, SP, RJ).</param>
    /// <param name="ambiente">Ambiente SEFAZ: Producao ou Homologacao.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Status retornado — campo <c>online</c> indica disponibilidade.</response>
    /// <response code="502">Falha na comunicação com a SEFAZ.</response>
    [HttpGet("status-servico")]
    [ProducesResponseType<StatusServicoSefaz>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ConsultarStatusServico(
        [FromQuery] UnidadeFederativa uf = UnidadeFederativa.MG,
        [FromQuery] AmbienteSefaz ambiente = AmbienteSefaz.Homologacao,
        CancellationToken ct = default)
    {
        var result = await consultarStatusServicoUseCase.ExecutarAsync(uf, ambiente, ct);

        if (result.IsFailure)
            return StatusCode(StatusCodes.Status502BadGateway, new { erro = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Consulta o retorno de um lote assíncrono pelo número de recibo (nRec).</summary>
    /// <remarks>
    /// Use quando a SEFAZ retornou status **103 (Lote Recebido)**.
    /// O `nRec` está no campo `numeroRecibo` da resposta do endpoint de emissão.
    ///
    /// Exemplo:
    ///
    ///     GET /api/v1/nfe/recibo/141240000001234?uf=MG&amp;ambiente=Homologacao
    /// </remarks>
    /// <param name="numeroRecibo">Número do recibo retornado pela SEFAZ no envio do lote.</param>
    /// <param name="uf">UF emissora (ex: MG, SP, RJ).</param>
    /// <param name="ambiente">Ambiente SEFAZ: Producao ou Homologacao.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Retorno processado — contém status e protocolos individuais por nota.</response>
    /// <response code="404">Recibo não encontrado ou erro na comunicação com a SEFAZ.</response>
    [HttpGet("recibo/{numeroRecibo}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarRecibo(
        string numeroRecibo,
        [FromQuery] UnidadeFederativa uf = UnidadeFederativa.MG,
        [FromQuery] AmbienteSefaz ambiente = AmbienteSefaz.Homologacao,
        CancellationToken ct = default)
    {
        var result = await consultarReciboUseCase.ExecutarAsync(numeroRecibo, uf, ambiente, ct);

        if (result.IsFailure)
            return NotFound(new { erro = result.Error });

        return Ok(result.Value);
    }
}
