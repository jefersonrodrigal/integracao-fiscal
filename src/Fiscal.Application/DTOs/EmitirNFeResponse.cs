using Fiscal.Domain.Enums;

namespace Fiscal.Application.DTOs;

public sealed class EmitirNFeResponse
{
    public bool Sucesso { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? NumeroProtocolo { get; set; }
    public string? NumeroRecibo { get; set; }
    public int CodigoStatus { get; set; }
    public string DescricaoStatus { get; set; } = string.Empty;
    public EstadoFiscal Estado { get; set; }
    public List<string> Erros { get; set; } = [];
    public string? XmlAutorizado { get; set; }
    public byte[]? DanfePdf { get; set; }
}
