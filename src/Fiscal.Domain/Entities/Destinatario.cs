using Fiscal.Domain.ValueObjects;

namespace Fiscal.Domain.Entities;

public sealed class Destinatario
{
    public Cnpj? Cnpj { get; set; }
    public Cpf? Cpf { get; set; }
    public string? IdEstrangeiro { get; set; }
    public string NomeRazaoSocial { get; set; } = string.Empty;
    public Endereco? Endereco { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// 0=Contribuinte ICMS, 1=Contribuinte Isento, 9=Não Contribuinte
    /// </summary>
    public int IndicadorIe { get; set; } = 9;
}
