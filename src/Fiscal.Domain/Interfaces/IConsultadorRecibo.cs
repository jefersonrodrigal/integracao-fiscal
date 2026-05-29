using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;

namespace Fiscal.Domain.Interfaces;

public interface IConsultadorRecibo
{
    Task<Result<ResultadoTransmissao>> ConsultarAsync(
        string numeroRecibo,
        UnidadeFederativa uf,
        AmbienteSefaz ambiente,
        CancellationToken ct = default);
}
