namespace Fiscal.Domain.Entities;

public sealed class Produto
{
    public string Codigo { get; set; } = string.Empty;
    public string Ean { get; set; } = "SEM GTIN";
    public string Descricao { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string? Nve { get; set; }
    public string Cfop { get; set; } = string.Empty;
    public string UnidadeComercial { get; set; } = string.Empty;
    public decimal QuantidadeComercial { get; set; }
    public decimal ValorUnitarioComercial { get; set; }
    public decimal ValorTotal { get; set; }
    public string EanTributavel { get; set; } = "SEM GTIN";
    public string UnidadeTributavel { get; set; } = string.Empty;
    public decimal QuantidadeTributavel { get; set; }
    public decimal ValorUnitarioTributavel { get; set; }
    public decimal? ValorFrete { get; set; }
    public decimal? ValorSeguro { get; set; }
    public decimal? ValorDesconto { get; set; }
    public decimal? ValorOutrasDespesas { get; set; }
    public bool CompoeTotalNF { get; set; } = true;
    public int NumeroItem { get; set; }
    public Imposto Imposto { get; set; } = new();
}
