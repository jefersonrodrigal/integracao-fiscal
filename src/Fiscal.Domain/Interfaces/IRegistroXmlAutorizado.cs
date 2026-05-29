using Fiscal.Domain.Common;

namespace Fiscal.Domain.Interfaces;

public interface IRegistroXmlAutorizado
{
    Task<Result> SalvarXmlAsync(string chaveAcesso, string xmlAutorizado, CancellationToken ct = default);
    Task<Result<string>> ObterXmlAsync(string chaveAcesso, CancellationToken ct = default);
}
