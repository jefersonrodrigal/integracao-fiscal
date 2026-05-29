using Fiscal.Domain.Enums;

namespace Fiscal.Domain.Entities;

public sealed class Lote
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public long IdLote { get; set; }
    public int IndicadorSincronico { get; set; } = 1; // 1=síncrono
    public AmbienteSefaz Ambiente { get; set; }
    public UnidadeFederativa Uf { get; set; }
    public List<NotaFiscal> Notas { get; set; } = [];
    public DateTime DataTransmissao { get; set; }
    public string? Recibo { get; set; }
    public string? XmlLote { get; set; }
}
