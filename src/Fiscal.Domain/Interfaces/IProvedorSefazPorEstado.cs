using Fiscal.Domain.Enums;

namespace Fiscal.Domain.Interfaces;

/// <summary>
/// Ponto de extensão central para múltiplos estados.
/// Cada UF deve implementar esta interface no Infrastructure.
/// </summary>
public interface IProvedorSefazPorEstado
{
    UnidadeFederativa Uf { get; }
    IConfiguracaoSefazProvider ObterConfiguracao(AmbienteSefaz ambiente);
    IEnumerable<RegraValidacaoEstadual> ObterRegrasValidacao();
}

public interface IConfiguracaoSefazProvider
{
    string EndpointAutorizacao { get; }
    string EndpointRetornoAutorizacao { get; }
    string EndpointConsultaProtocolo { get; }
    string EndpointConsultaCadastro { get; }
    string EndpointStatusServico { get; }
    string EndpointRecepcaoEvento { get; }
    string SoapActionAutorizacao { get; }
    string SoapActionRetornoAutorizacao { get; }
    string SoapActionConsultaProtocolo { get; }
    string SoapActionStatusServico { get; }
    AmbienteSefaz Ambiente { get; }
    string VersaoLayout { get; }
    string PathSchemaXsd { get; }
}

public abstract record RegraValidacaoEstadual
{
    public abstract string Codigo { get; }
    public abstract string Descricao { get; }
    public abstract string ReferenciaLegal { get; }
}
