using Fiscal.Domain.Enums;

namespace Fiscal.Infrastructure.Providers.MG;

/// <summary>
/// Endpoints oficiais da SEFAZ-MG para NF-e 4.00.
/// Fonte: Portal da NF-e (www.nfe.fazenda.gov.br) e Portal SEFAZ-MG.
/// Sempre verificar atualizações em notas técnicas ENCAT antes de produção.
/// Última revisão de conformidade: NT 2024.001 / MOC 7.0.
/// </summary>
internal static class MgEndpointsSefaz
{
    private const string BaseHomologacao = "https://hnfe.fazenda.mg.gov.br/nfe2/services";
    private const string BaseProducao = "https://nfe.fazenda.mg.gov.br/nfe2/services";

    public static EndpointsNFe ObterEndpoints(AmbienteSefaz ambiente)
    {
        var b = ambiente == AmbienteSefaz.Homologacao ? BaseHomologacao : BaseProducao;
        return new EndpointsNFe
        {
            Autorizacao = $"{b}/NFeAutorizacao4",
            RetornoAutorizacao = $"{b}/NFeRetAutorizacao4",
            ConsultaProtocolo = $"{b}/NFeConsultaProtocolo4",
            ConsultaCadastro = $"{b}/CadConsultaCadastro4",
            StatusServico = $"{b}/NFeStatusServico4",
            RecepcaoEvento = $"{b}/NFeRecepcaoEvento4",
            InutilizacaoNFe = $"{b}/NFeInutilizacao4"
        };
    }
}

internal sealed class EndpointsNFe
{
    public string Autorizacao { get; init; } = string.Empty;
    public string RetornoAutorizacao { get; init; } = string.Empty;
    public string ConsultaProtocolo { get; init; } = string.Empty;
    public string ConsultaCadastro { get; init; } = string.Empty;
    public string StatusServico { get; init; } = string.Empty;
    public string RecepcaoEvento { get; init; } = string.Empty;
    public string InutilizacaoNFe { get; init; } = string.Empty;
}
