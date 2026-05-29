namespace Fiscal.Domain.Entities;

public sealed class ImpostoIcms
{
    public string Origem { get; set; } = "0";
    public string Cst { get; set; } = string.Empty;
    public string Csosn { get; set; } = string.Empty;
    public string ModalidadeBc { get; set; } = "3";
    public decimal? PercentualReducaoBc { get; set; }
    public decimal? BaseCalculo { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }

    // Substituição Tributária
    public string ModalidadeBcSt { get; set; } = "4";
    public decimal? PercentualMvaSt { get; set; }
    public decimal? PercentualReducaoBcSt { get; set; }
    public decimal? BaseCalculoSt { get; set; }
    public decimal? AliquotaSt { get; set; }
    public decimal? ValorSt { get; set; }
    public decimal? BaseCalculoFcpSt { get; set; }
    public decimal? PercentualFcpSt { get; set; }
    public decimal? ValorFcpSt { get; set; }

    // ST retido anteriormente (CST 60 / CSOSN 500)
    public decimal? BaseCalculoStRetido { get; set; }
    public decimal? PercentualSt { get; set; }
    public decimal? ValorIcmsStRetido { get; set; }
    public decimal? BaseCalculoFcpStRetido { get; set; }
    public decimal? PercentualFcpStRetido { get; set; }
    public decimal? ValorFcpStRetido { get; set; }

    // Desoneração
    public decimal? ValorIcmsDesonerado { get; set; }
    public string? MotivoDesoneracaoIcms { get; set; }

    // Diferimento (CST 51)
    public decimal? ValorIcmsOp { get; set; }
    public decimal? PercentualDiferimento { get; set; }
    public decimal? ValorIcmsDiferido { get; set; }
    public decimal? BaseCalculoFcp { get; set; }
    public decimal? PercentualFcp { get; set; }
    public decimal? ValorFcp { get; set; }

    // Simples Nacional
    public decimal? PercentualCredSN { get; set; }
    public decimal? ValorCredIcmsSN { get; set; }
}

public sealed class ImpostoPis
{
    public string Cst { get; set; } = string.Empty;
    public decimal? BaseCalculo { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }
    public decimal? QuantidadeVendida { get; set; }
    public decimal? AliquotaReais { get; set; }
}

public sealed class ImpostoCofins
{
    public string Cst { get; set; } = string.Empty;
    public decimal? BaseCalculo { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }
    public decimal? QuantidadeVendida { get; set; }
    public decimal? AliquotaReais { get; set; }
}

public sealed class ImpostoIpi
{
    public string CnpjProdutor { get; set; } = string.Empty;
    public string CodigoSelo { get; set; } = string.Empty;
    public int QuantidadeSelo { get; set; }
    public string CodigoEnquadramento { get; set; } = string.Empty;
    public string Cst { get; set; } = string.Empty;
    public decimal? BaseCalculo { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }
}

public sealed class Imposto
{
    public decimal? ValorTotalTributos { get; set; }
    public ImpostoIcms Icms { get; set; } = new();
    public ImpostoPis? Pis { get; set; }
    public ImpostoCofins? Cofins { get; set; }
    public ImpostoIpi? Ipi { get; set; }
}
