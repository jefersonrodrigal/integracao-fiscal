using Fiscal.Domain.Enums;

namespace Fiscal.Application.DTOs;

public sealed class EmitirNFeRequest
{
    public TipoDocumentoFiscal Modelo { get; set; } = TipoDocumentoFiscal.NFe;
    public AmbienteSefaz Ambiente { get; set; } = AmbienteSefaz.Homologacao;
    public UnidadeFederativa Uf { get; set; } = UnidadeFederativa.MG;
    public EmitenteDtoRequest Emitente { get; set; } = new();
    public DestinatarioDtoRequest? Destinatario { get; set; }
    public List<ProdutoDtoRequest> Produtos { get; set; } = [];
    public string NaturezaOperacao { get; set; } = string.Empty;
    public int Serie { get; set; } = 1;
    public int Numero { get; set; }
    public string? InformacoesAdicionais { get; set; }
    public int FinalidadeEmissao { get; set; } = 1;
    public int IndicadorPresencaComprador { get; set; } = 1;
    /// <summary>
    /// Formas de pagamento. Se vazio, será gerado um detalhamento padrão
    /// com tPag=90 (Sem Pagamento) e vPag=0.00, válido para NF-e B2B.
    /// </summary>
    public List<PagamentoDtoRequest> Pagamentos { get; set; } = [];
}

public sealed class EmitenteDtoRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public EnderecoDtoRequest Endereco { get; set; } = new();
    public string InscricaoEstadual { get; set; } = string.Empty;
    public string CnaeCode { get; set; } = string.Empty;
    public string CodigoRegimeTributario { get; set; } = "1";
}

public sealed class DestinatarioDtoRequest
{
    public string? Cnpj { get; set; }
    public string? Cpf { get; set; }
    public string NomeRazaoSocial { get; set; } = string.Empty;
    public EnderecoDtoRequest? Endereco { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Email { get; set; }
    public int IndicadorIe { get; set; } = 9;
}

public sealed class EnderecoDtoRequest
{
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string CodigoMunicipio { get; set; } = string.Empty;
    public string NomeMunicipio { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string? Telefone { get; set; }
}

public sealed class ProdutoDtoRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string Cfop { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorDesconto { get; set; }
    public ImpostoDtoRequest Imposto { get; set; } = new();
}

public sealed class PagamentoDtoRequest
{
    /// <summary>Forma de pagamento (2 dígitos). Valores comuns: "01"=Dinheiro, "03"=Cartão de Crédito, "90"=Sem Pagamento.</summary>
    public string TipoPagamento { get; set; } = "90";
    public decimal ValorPagamento { get; set; }
    /// <summary>0=À vista; 1=A prazo. Opcional.</summary>
    public string? IndicadorPagamento { get; set; }
}

public sealed class ImpostoDtoRequest
{
    public string Origem { get; set; } = "0";
    /// <summary>CST (regime normal) ou CSOSN (Simples Nacional). Exemplos: "00","20","60","400","500".</summary>
    public string CstCsosn { get; set; } = string.Empty;
    public decimal? BaseCalculoIcms { get; set; }
    public decimal? AliquotaIcms { get; set; }
    public decimal? PercentualReducaoBc { get; set; }
    // Substituição Tributária
    public decimal? BaseCalculoIcmsSt { get; set; }
    public decimal? AliquotaIcmsSt { get; set; }
    public decimal? PercentualMvaSt { get; set; }
    // Simples Nacional — crédito (CSOSN 101/201/900)
    public decimal? PercentualCredSN { get; set; }
    public string CstPis { get; set; } = "07";
    public string CstCofins { get; set; } = "07";
}
