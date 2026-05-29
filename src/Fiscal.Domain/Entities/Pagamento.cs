namespace Fiscal.Domain.Entities;

public sealed class DetalhamentoPagamento
{
    /// <summary>0=À vista; 1=A prazo</summary>
    public string? IndicadorPagamento { get; set; }
    /// <summary>Forma de pagamento (2 dígitos). Ex: "01"=Dinheiro, "90"=Sem pagamento.</summary>
    public string TipoPagamento { get; set; } = "90";
    public decimal ValorPagamento { get; set; }
}

public sealed class Pagamento
{
    public List<DetalhamentoPagamento> Detalhamentos { get; set; } = [];
}
