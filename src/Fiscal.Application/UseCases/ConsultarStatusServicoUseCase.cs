using Fiscal.Domain.Common;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;

namespace Fiscal.Application.UseCases;

public sealed class ConsultarStatusServicoUseCase(IConsultadorStatusServico consultador)
{
    public Task<Result<StatusServicoSefaz>> ExecutarAsync(
        UnidadeFederativa uf, AmbienteSefaz ambiente, CancellationToken ct = default)
        => consultador.ConsultarAsync(uf, ambiente, ct);
}
