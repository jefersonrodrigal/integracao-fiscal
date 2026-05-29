using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;

namespace Fiscal.Domain.Interfaces;

public interface IGeradorDanfe
{
    Task<Result<byte[]>> GerarAsync(NotaFiscal nota, TipoDocumentoFiscal tipo, CancellationToken ct = default);
    Task<Result<byte[]>> GerarDeXmlAsync(string xmlAutorizado, TipoDocumentoFiscal tipo, CancellationToken ct = default);
}
