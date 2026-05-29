using Fiscal.Domain.Common;
using Fiscal.Domain.Enums;

namespace Fiscal.Domain.Interfaces;

public interface IConsultadorStatusServico
{
    Task<Result<StatusServicoSefaz>> ConsultarAsync(UnidadeFederativa uf, AmbienteSefaz ambiente, CancellationToken ct = default);
}

public sealed class StatusServicoSefaz
{
    public int CodigoStatus { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? DataHoraRetorno { get; set; }
    public int? TempoMedio { get; set; }
    public bool Online => CodigoStatus == 107;
}
