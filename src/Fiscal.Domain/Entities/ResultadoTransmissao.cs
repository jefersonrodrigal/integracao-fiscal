namespace Fiscal.Domain.Entities;

public sealed class ResultadoTransmissao
{
    public int CodigoStatus { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? NumeroRecibo { get; set; }
    public List<ResultadoNota> Resultados { get; set; } = [];
    public string? XmlRetornoSefaz { get; set; }
    public bool Sucesso => CodigoStatus is 100 or 103 or 104;
}

public sealed class ResultadoNota
{
    public string ChaveAcesso { get; set; } = string.Empty;
    public int CodigoStatus { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public ProtocoloAutorizacao? Protocolo { get; set; }
}
