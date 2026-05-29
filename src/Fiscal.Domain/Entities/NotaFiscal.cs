using Fiscal.Domain.Enums;
using Fiscal.Domain.ValueObjects;

namespace Fiscal.Domain.Entities;

public sealed class NotaFiscal
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public ChaveAcesso? ChaveAcesso { get; set; }
    public TipoDocumentoFiscal Modelo { get; set; }
    public AmbienteSefaz Ambiente { get; set; }
    public EstadoFiscal Estado { get; private set; } = EstadoFiscal.Digitacao;
    public UnidadeFederativa Uf { get; set; }

    // Identificação
    public int CodigoUf { get; set; }
    public string NaturezaOperacao { get; set; } = string.Empty;
    public int Serie { get; set; }
    public int Numero { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime? DataSaidaEntrada { get; set; }
    public string HoraSaidaEntrada { get; set; } = string.Empty;
    public int TipoNf { get; set; } = 1; // 0=Entrada, 1=Saída
    public int IndicadorDestinatario { get; set; } = 1; // 1=Interna, 2=Interestadual, 3=Exterior
    public TipoEmissao TipoEmissao { get; set; } = TipoEmissao.Normal;
    public string VersaoLayout { get; set; } = "4.00";
    public int FinalidadeEmissao { get; set; } = 1; // 1=Normal, 2=Complementar, 3=Ajuste, 4=Devolução
    public int IndicadorPresencaComprador { get; set; } = 1;
    public int IndicadorIntermediario { get; set; } = 0;
    public int ProcessoEmissao { get; set; } = 0;
    public string VersaoProcesso { get; set; } = string.Empty;
    public int CodigoNumerico { get; set; }

    public Emitente Emitente { get; set; } = new();
    public Destinatario? Destinatario { get; set; }
    public List<Produto> Produtos { get; set; } = [];
    public Totais Totais { get; set; } = new();
    public Pagamento Pagamento { get; set; } = new();
    public string? InformacoesAdicionaisFisco { get; set; }
    public string? InformacoesAdicionaisContribuinte { get; set; }

    // XML e protocolo
    public string? XmlGerado { get; set; }
    public string? XmlAssinado { get; set; }
    public string? XmlAutorizado { get; set; }
    public ProtocoloAutorizacao? Protocolo { get; set; }

    public void TransicionarEstado(EstadoFiscal novoEstado) => Estado = novoEstado;

    public void Autorizar(ProtocoloAutorizacao protocolo)
    {
        Protocolo = protocolo;
        Estado = EstadoFiscal.Autorizada;
    }

    public void Rejeitar(string motivo) => Estado = EstadoFiscal.Rejeitada;
}
