namespace Fiscal.Domain.Entities;

public sealed class Recibo
{
    public string NumeroRecibo { get; set; } = string.Empty;
    public DateTime DataRecebimento { get; set; }
    public int CodigoMensagem { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int CodigoStatus { get; set; }
    public string MotivoCodigo { get; set; } = string.Empty;
    public DateTime? TempoMedioResposta { get; set; }
    public string? XmlRetorno { get; set; }
}
