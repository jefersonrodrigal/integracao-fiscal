using Fiscal.Domain.Common;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fiscal.Infrastructure.Persistence;

public sealed class RegistroXmlAutorizadoArquivo(
    IOptions<StorageOptions> options,
    ILogger<RegistroXmlAutorizadoArquivo> logger) : IRegistroXmlAutorizado
{
    private readonly string _basePath = options.Value.XmlAutorizadoPath;

    public async Task<Result> SalvarXmlAsync(string chaveAcesso, string xmlAutorizado, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_basePath);
            var path = Path.Combine(_basePath, $"{chaveAcesso}-nfe.xml");
            await File.WriteAllTextAsync(path, xmlAutorizado, System.Text.Encoding.UTF8, ct);
            logger.LogInformation("XML autorizado salvo: {Path}", path);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao salvar XML autorizado para chave {Chave}", chaveAcesso);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<string>> ObterXmlAsync(string chaveAcesso, CancellationToken ct = default)
    {
        var path = Path.Combine(_basePath, $"{chaveAcesso}-nfe.xml");
        if (!File.Exists(path))
            return Result<string>.Failure($"XML não encontrado para chave {chaveAcesso}.");

        var xml = await File.ReadAllTextAsync(path, ct);
        return Result<string>.Success(xml);
    }
}
