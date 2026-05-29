using FluentAssertions;
using Fiscal.Domain.Entities;
using Fiscal.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fiscal.Infrastructure.Tests.Persistence;

public sealed class RepositorioProtocoloArquivoTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    private RepositorioProtocoloArquivo CriarRepositorio()
    {
        var opts = Options.Create(new StorageOptions { ProtocolosPath = _tempPath });
        return new RepositorioProtocoloArquivo(opts, NullLogger<RepositorioProtocoloArquivo>.Instance);
    }

    [Fact]
    public async Task SalvarAsync_deve_criar_arquivo_json()
    {
        var repo = CriarRepositorio();
        var protocolo = CriarProtocolo();

        var result = await repo.SalvarAsync(protocolo);

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(_tempPath, $"{protocolo.ChaveAcesso}.json")).Should().BeTrue();
    }

    [Fact]
    public async Task ObterPorChaveAsync_deve_retornar_protocolo_salvo()
    {
        var repo = CriarRepositorio();
        var protocolo = CriarProtocolo();
        await repo.SalvarAsync(protocolo);

        var result = await repo.ObterPorChaveAsync(protocolo.ChaveAcesso);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NumeroProtocolo.Should().Be(protocolo.NumeroProtocolo);
        result.Value!.CodigoStatus.Should().Be(100);
    }

    [Fact]
    public async Task ObterPorChaveAsync_deve_retornar_falha_quando_nao_existe()
    {
        var repo = CriarRepositorio();

        var result = await repo.ObterPorChaveAsync("chaveInexistente");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ObterPorNumeroAsync_deve_encontrar_protocolo()
    {
        var repo = CriarRepositorio();
        var protocolo = CriarProtocolo();
        await repo.SalvarAsync(protocolo);

        var result = await repo.ObterPorNumeroAsync("141240000001234");

        result.IsSuccess.Should().BeTrue();
    }

    private static ProtocoloAutorizacao CriarProtocolo() => new()
    {
        NumeroProtocolo = "141240000001234",
        ChaveAcesso = "31240511222333000181550010000000011123456785",
        CodigoStatus = 100,
        DescricaoStatus = "Autorizado o uso da NF-e",
        DataHoraRecebimento = DateTime.UtcNow
    };

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, recursive: true);
    }
}
