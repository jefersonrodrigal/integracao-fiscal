using Fiscal.Domain.ValueObjects;

namespace Fiscal.Domain.Entities;

public sealed class Emitente
{
    public Cnpj Cnpj { get; set; } = null!;
    public string? Cpf { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public Endereco Endereco { get; set; } = new();
    public string InscricaoEstadual { get; set; } = string.Empty;
    public string? InscricaoEstadualSt { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string CnaeCode { get; set; } = string.Empty;
    public string CodigoRegimeTributario { get; set; } = "1"; // 1=Simples, 2=SimplesExcesso, 3=Normal
}
