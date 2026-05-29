namespace Fiscal.Domain.Entities;

public sealed class Endereco
{
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string CodigoMunicipio { get; set; } = string.Empty;
    public string NomeMunicipio { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string CodigoPais { get; set; } = "1058";
    public string NomePais { get; set; } = "Brasil";
    public string? Telefone { get; set; }
}
