using Fiscal.Application.DTOs;
using Fiscal.Application.Services;
using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fiscal.Application.UseCases;

public sealed class EmitirNFeUseCase(
    IGeradorXmlNFe geradorXml,
    IValidadorXsd validadorXsd,
    IAssinadorXml assinadorXml,
    IAutorizadorNFe autorizadorNFe,
    IRepositorioProtocolo repositorioProtocolo,
    IRegistroXmlAutorizado registroXml,
    IGeradorDanfe geradorDanfe,
    ILogger<EmitirNFeUseCase> logger)
{
    public async Task<Result<EmitirNFeResponse>> ExecutarAsync(EmitirNFeRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Iniciando emissão de NF-e/NFC-e. Ambiente={Ambiente} UF={Uf}", request.Ambiente, request.Uf);

        // 1. Criar entidade a partir do DTO
        NotaFiscal nota;
        try
        {
            nota = NotaFiscalFactory.Criar(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao criar entidade NotaFiscal");
            return Result<EmitirNFeResponse>.Failure($"Dados inválidos: {ex.Message}");
        }

        // 2. Gerar XML
        var xmlResult = geradorXml.Gerar(nota);
        if (xmlResult.IsFailure)
        {
            logger.LogWarning("Falha na geração do XML: {Erro}", xmlResult.Error);
            return Result<EmitirNFeResponse>.Failure(xmlResult.Error);
        }
        nota.XmlGerado = xmlResult.Value;
        nota.TransicionarEstado(EstadoFiscal.XmlGerado);

        // 3. Assinar XML
        var assinaturaResult = assinadorXml.Assinar(nota.XmlGerado!);
        if (assinaturaResult.IsFailure)
        {
            logger.LogWarning("Falha na assinatura XML: {Erro}", assinaturaResult.Error);
            return Result<EmitirNFeResponse>.Failure(assinaturaResult.Error);
        }
        nota.XmlAssinado = assinaturaResult.Value;
        nota.TransicionarEstado(EstadoFiscal.XmlAssinado);

        // 4. Validar XML assinado contra XSD (ds:Signature é obrigatório no schema)
        var validacaoResult = validadorXsd.Validar(nota.XmlAssinado!, nota.Modelo, nota.VersaoLayout);
        if (validacaoResult.IsFailure)
        {
            logger.LogWarning("XML inválido contra XSD: {Erros}", string.Join("; ", validacaoResult.Errors));
            return Result<EmitirNFeResponse>.Failure(validacaoResult.Errors);
        }
        nota.TransicionarEstado(EstadoFiscal.XmlValidado);

        // 5. Transmitir e obter autorização
        var autorizacaoResult = await autorizadorNFe.AutorizarAsync(nota, ct);
        if (autorizacaoResult.IsFailure)
        {
            logger.LogWarning("Falha na transmissão: {Erro}", autorizacaoResult.Error);
            return Result<EmitirNFeResponse>.Failure(autorizacaoResult.Error);
        }

        var resultado = autorizacaoResult.Value!;

        if (!resultado.Sucesso)
        {
            nota.TransicionarEstado(EstadoFiscal.Rejeitada);
            return Result<EmitirNFeResponse>.Success(new EmitirNFeResponse
            {
                Sucesso = false,
                CodigoStatus = resultado.CodigoStatus,
                DescricaoStatus = resultado.Descricao,
                Estado = nota.Estado,
                Erros = [resultado.Descricao]
            });
        }

        // 6. Processar protocolo de autorização
        var resultadoNota = resultado.Resultados.FirstOrDefault();
        if (resultadoNota?.Protocolo is null)
            return Result<EmitirNFeResponse>.Failure("Protocolo de autorização não encontrado no retorno da SEFAZ.");

        nota.Autorizar(resultadoNota.Protocolo);

        // 7. Salvar protocolo
        var salvarResult = await repositorioProtocolo.SalvarAsync(nota.Protocolo!, ct);
        if (salvarResult.IsFailure)
            logger.LogWarning("Falha ao salvar protocolo: {Erro}", salvarResult.Error);

        // 8. Salvar XML autorizado
        if (nota.XmlAutorizado is not null)
        {
            var xmlSaveResult = await registroXml.SalvarXmlAsync(nota.ChaveAcesso!.Valor, nota.XmlAutorizado, ct);
            if (xmlSaveResult.IsFailure)
                logger.LogWarning("Falha ao salvar XML autorizado: {Erro}", xmlSaveResult.Error);
        }

        // 9. Gerar DANFE/DANFCE
        byte[]? danfePdf = null;
        var danfeResult = await geradorDanfe.GerarAsync(nota, nota.Modelo, ct);
        if (danfeResult.IsSuccess)
            danfePdf = danfeResult.Value;
        else
            logger.LogWarning("Falha ao gerar DANFE: {Erro}", danfeResult.Error);

        logger.LogInformation(
            "NF-e autorizada com sucesso. Chave={Chave} Protocolo={Protocolo}",
            nota.ChaveAcesso?.Valor, nota.Protocolo?.NumeroProtocolo);

        return Result<EmitirNFeResponse>.Success(new EmitirNFeResponse
        {
            Sucesso = true,
            ChaveAcesso = nota.ChaveAcesso?.Valor,
            NumeroProtocolo = nota.Protocolo?.NumeroProtocolo,
            CodigoStatus = nota.Protocolo?.CodigoStatus ?? 0,
            DescricaoStatus = nota.Protocolo?.DescricaoStatus ?? string.Empty,
            Estado = nota.Estado,
            XmlAutorizado = nota.XmlAutorizado,
            DanfePdf = danfePdf
        });
    }
}
