using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;

namespace Fiscal.Domain.Interfaces;

public interface IRepositorioProtocolo
{
    Task<Result> SalvarAsync(ProtocoloAutorizacao protocolo, CancellationToken ct = default);
    Task<Result<ProtocoloAutorizacao>> ObterPorChaveAsync(string chaveAcesso, CancellationToken ct = default);
    Task<Result<ProtocoloAutorizacao>> ObterPorNumeroAsync(string numeroProtocolo, CancellationToken ct = default);
}
