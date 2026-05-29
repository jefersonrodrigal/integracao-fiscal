namespace Fiscal.Domain.Entities;

public sealed class TotaisIcms
{
    public decimal BaseCalculoIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal BaseCalculoIcmsSt { get; set; }
    public decimal ValorIcmsSt { get; set; }
    public decimal ValorFcpSt { get; set; }
    public decimal ValorFcpStRetido { get; set; }
    public decimal ValorProdutos { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorIi { get; set; }
    public decimal ValorIpi { get; set; }
    public decimal ValorIpiDevolvido { get; set; }
    public decimal ValorPis { get; set; }
    public decimal ValorCofins { get; set; }
    public decimal ValorOutrasDespesas { get; set; }
    public decimal ValorNf { get; set; }
    public decimal ValorTotalTributos { get; set; }
}

public sealed class Totais
{
    public TotaisIcms IcmsTotais { get; set; } = new();
}
