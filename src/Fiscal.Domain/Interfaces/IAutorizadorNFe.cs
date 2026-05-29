using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;

namespace Fiscal.Domain.Interfaces;

public interface IAutorizadorNFe
{
    Task<Result<ResultadoTransmissao>> AutorizarAsync(NotaFiscal nota, CancellationToken ct = default);
    Task<Result<ResultadoTransmissao>> AutorizarLoteAsync(Lote lote, CancellationToken ct = default);
}
