using Fiscal.Domain.Common;
using Fiscal.Domain.Enums;

namespace Fiscal.Domain.Interfaces;

public interface IClienteSoapSefaz
{
    Task<Result<string>> EnviarAsync(
        string xmlEnvelope,
        string endpoint,
        string soapAction,
        CancellationToken ct = default);
}
