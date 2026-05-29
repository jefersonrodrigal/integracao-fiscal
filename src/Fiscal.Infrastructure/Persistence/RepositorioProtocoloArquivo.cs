using System.Text.Json;
using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fiscal.Infrastructure.Persistence;

/// <summary>
/// Persiste protocolos de autorização em arquivos JSON no sistema de arquivos.
/// Substitua por implementação com banco de dados em produção.
/// Estrutura: {BasePath}/{chaveAcesso}.json e {BasePath}/{numeroProtocolo}.json
/// </summary>
public sealed class RepositorioProtocoloArquivo(
    IOptions<StorageOptions> options,
    ILogger<RepositorioProtocoloArquivo> logger) : IRepositorioProtocolo
{
    private readonly string _basePath = options.Value.ProtocolosPath;

    public async Task<Result> SalvarAsync(ProtocoloAutorizacao protocolo, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_basePath);
            var json = JsonSerializer.Serialize(protocolo, JsonOpts.Default);

            var fileByChave = Path.Combine(_basePath, $"{protocolo.ChaveAcesso}.json");
            await File.WriteAllTextAsync(fileByChave, json, ct);

            logger.LogInformation("Protocolo {Protocolo} salvo para chave {Chave}", protocolo.NumeroProtocolo, protocolo.ChaveAcesso);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao salvar protocolo {Protocolo}", protocolo.NumeroProtocolo);
            return Result.Failure($"Falha ao persistir protocolo: {ex.Message}");
        }
    }

    public async Task<Result<ProtocoloAutorizacao>> ObterPorChaveAsync(string chaveAcesso, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, $"{chaveAcesso}.json");
        return await LerArquivo(filePath, ct);
    }

    public async Task<Result<ProtocoloAutorizacao>> ObterPorNumeroAsync(string numeroProtocolo, CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_basePath, "*.json");
        foreach (var f in files)
        {
            var result = await LerArquivo(f, ct);
            if (result.IsSuccess && result.Value!.NumeroProtocolo == numeroProtocolo)
                return result;
        }
        return Result<ProtocoloAutorizacao>.Failure($"Protocolo {numeroProtocolo} não encontrado.");
    }

    private async Task<Result<ProtocoloAutorizacao>> LerArquivo(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            return Result<ProtocoloAutorizacao>.Failure("Protocolo não encontrado.");

        var json = await File.ReadAllTextAsync(path, ct);
        var protocolo = JsonSerializer.Deserialize<ProtocoloAutorizacao>(json, JsonOpts.Default);
        return protocolo is null
            ? Result<ProtocoloAutorizacao>.Failure("Falha ao desserializar protocolo.")
            : Result<ProtocoloAutorizacao>.Success(protocolo);
    }
}

internal static class JsonOpts
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
