using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;

namespace Fiscal.Infrastructure.Providers.MG;

/// <summary>
/// Provedor SEFAZ para Minas Gerais (MG).
/// Implementa IProvedorSefazPorEstado como Strategy para a UF.
/// Para adicionar nova UF, crie XxSefazProvider análogo e registre no DI.
/// </summary>
public sealed class MgSefazProvider : IProvedorSefazPorEstado
{
    public UnidadeFederativa Uf => UnidadeFederativa.MG;

    public IConfiguracaoSefazProvider ObterConfiguracao(AmbienteSefaz ambiente)
        => new MgConfiguracaoSefaz(ambiente);

    public IEnumerable<RegraValidacaoEstadual> ObterRegrasValidacao() =>
    [
        new MgInscricaoEstadualObrigatoria(),
        new MgCfopInterestadualRestritoNFCe(),
        new MgIeDestinatarioNaoContribuinte()
    ];
}
