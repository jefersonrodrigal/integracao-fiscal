namespace Fiscal.Domain.Entities;

public sealed class ProtocoloAutorizacao
{
    public string NumeroProtocolo { get; set; } = string.Empty;
    public DateTime DataHoraRecebimento { get; set; }
    public int CodigoStatus { get; set; }
    public string DescricaoStatus { get; set; } = string.Empty;
    public string ChaveAcesso { get; set; } = string.Empty;
    public string? DigestValue { get; set; }
    public string? XmlProtocolo { get; set; }
    public string? XmlNfAutorizado { get; set; }
}
