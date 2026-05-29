using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;

namespace Fiscal.Infrastructure.Providers.MG;

/// <summary>
/// Configuração de endpoints e parâmetros SOAP da SEFAZ-MG.
/// SoapActions baseados no WSDL oficial NF-e 4.00 ENCAT.
/// </summary>
internal sealed class MgConfiguracaoSefaz : IConfiguracaoSefazProvider
{
    private readonly EndpointsNFe _endpoints;

    public MgConfiguracaoSefaz(AmbienteSefaz ambiente)
    {
        Ambiente = ambiente;
        _endpoints = MgEndpointsSefaz.ObterEndpoints(ambiente);
    }

    public AmbienteSefaz Ambiente { get; }
    public string VersaoLayout => "4.00";

    public string EndpointAutorizacao => _endpoints.Autorizacao;
    public string EndpointRetornoAutorizacao => _endpoints.RetornoAutorizacao;
    public string EndpointConsultaProtocolo => _endpoints.ConsultaProtocolo;
    public string EndpointConsultaCadastro => _endpoints.ConsultaCadastro;
    public string EndpointStatusServico => _endpoints.StatusServico;
    public string EndpointRecepcaoEvento => _endpoints.RecepcaoEvento;

    // SoapActions conforme WSDL oficial ENCAT NF-e 4.00
    public string SoapActionAutorizacao => "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4/nfeAutorizacaoLote";
    public string SoapActionRetornoAutorizacao => "http://www.portalfiscal.inf.br/nfe/wsdl/NFeRetAutorizacao4/nfeRetAutorizacaoLote";
    public string SoapActionConsultaProtocolo => "http://www.portalfiscal.inf.br/nfe/wsdl/NFeConsultaProtocolo4/nfeConsultaNF";
    public string SoapActionStatusServico => "http://www.portalfiscal.inf.br/nfe/wsdl/NFeStatusServico4/nfeStatusServicoNF";

    public string PathSchemaXsd => Path.Combine(
        AppContext.BaseDirectory, "Schemas", "NFe", VersaoLayout);
}
