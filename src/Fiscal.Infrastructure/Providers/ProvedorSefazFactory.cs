using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;

namespace Fiscal.Infrastructure.Providers;

/// <summary>
/// Fábrica que resolve o IProvedorSefazPorEstado correto para cada UF.
/// Novos estados são registrados via DI — sem alterar esta classe.
/// </summary>
public sealed class ProvedorSefazFactory(IEnumerable<IProvedorSefazPorEstado> provedores)
{
    private readonly Dictionary<UnidadeFederativa, IProvedorSefazPorEstado> _map =
        provedores.ToDictionary(p => p.Uf);

    public IProvedorSefazPorEstado Resolver(UnidadeFederativa uf)
    {
        if (_map.TryGetValue(uf, out var provedor))
            return provedor;

        throw new NotSupportedException(
            $"UF '{uf}' não possui provedor SEFAZ implementado. " +
            $"Implemente IProvedorSefazPorEstado e registre no contêiner de DI.");
    }

    public bool Suporta(UnidadeFederativa uf) => _map.ContainsKey(uf);
}
