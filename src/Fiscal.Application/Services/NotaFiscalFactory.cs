using Fiscal.Application.DTOs;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.ValueObjects;

namespace Fiscal.Application.Services;

public static class NotaFiscalFactory
{
    public static NotaFiscal Criar(EmitirNFeRequest request)
    {
        var nota = new NotaFiscal
        {
            Modelo = request.Modelo,
            Ambiente = request.Ambiente,
            Uf = request.Uf,
            CodigoUf = (int)request.Uf,
            NaturezaOperacao = request.NaturezaOperacao,
            Serie = request.Serie,
            Numero = request.Numero,
            DataEmissao = DateTime.UtcNow,
            FinalidadeEmissao = request.FinalidadeEmissao,
            IndicadorPresencaComprador = request.IndicadorPresencaComprador,
            InformacoesAdicionaisContribuinte = request.InformacoesAdicionais,
            CodigoNumerico = GerarCodigoNumerico()
        };

        nota.Emitente = MapEmitente(request.Emitente);

        if (request.Destinatario is not null)
            nota.Destinatario = MapDestinatario(request.Destinatario);

        int item = 1;
        foreach (var p in request.Produtos)
        {
            nota.Produtos.Add(MapProduto(p, item++));
        }

        nota.Totais = CalcularTotais(nota.Produtos);
        nota.Pagamento = MapPagamento(request.Pagamentos, nota.Totais.IcmsTotais.ValorNf);

        nota.ChaveAcesso = ChaveAcesso.Criar(
            nota.CodigoUf,
            int.Parse(nota.DataEmissao.ToString("yyMM")),
            nota.Emitente.Cnpj.Valor,
            (int)nota.Modelo,
            nota.Serie,
            nota.Numero,
            (int)nota.TipoEmissao,
            nota.CodigoNumerico);

        return nota;
    }

    private static Emitente MapEmitente(EmitenteDtoRequest dto) => new()
    {
        Cnpj = new Cnpj(dto.Cnpj),
        RazaoSocial = dto.RazaoSocial,
        NomeFantasia = dto.NomeFantasia,
        InscricaoEstadual = dto.InscricaoEstadual,
        CnaeCode = dto.CnaeCode,
        CodigoRegimeTributario = dto.CodigoRegimeTributario,
        Endereco = MapEndereco(dto.Endereco)
    };

    private static Destinatario MapDestinatario(DestinatarioDtoRequest dto)
    {
        var dest = new Destinatario
        {
            NomeRazaoSocial = dto.NomeRazaoSocial,
            InscricaoEstadual = dto.InscricaoEstadual,
            Email = dto.Email,
            IndicadorIe = dto.IndicadorIe
        };
        if (!string.IsNullOrWhiteSpace(dto.Cnpj)) dest.Cnpj = new Cnpj(dto.Cnpj);
        if (!string.IsNullOrWhiteSpace(dto.Cpf)) dest.Cpf = new Cpf(dto.Cpf);
        if (dto.Endereco is not null) dest.Endereco = MapEndereco(dto.Endereco);
        return dest;
    }

    private static Endereco MapEndereco(EnderecoDtoRequest dto) => new()
    {
        Logradouro = dto.Logradouro,
        Numero = dto.Numero,
        Complemento = dto.Complemento,
        Bairro = dto.Bairro,
        CodigoMunicipio = dto.CodigoMunicipio,
        NomeMunicipio = dto.NomeMunicipio,
        Uf = dto.Uf,
        Cep = new string(dto.Cep.Where(char.IsDigit).ToArray()),
        Telefone = dto.Telefone
    };

    private static readonly HashSet<string> _csosnCodes =
        ["101", "102", "103", "201", "202", "203", "300", "400", "500", "900"];

    private static Produto MapProduto(ProdutoDtoRequest dto, int numeroItem)
    {
        decimal valorTotal = Math.Round(dto.Quantidade * dto.ValorUnitario - dto.ValorDesconto, 2);

        var ehCsosn = _csosnCodes.Contains(dto.Imposto.CstCsosn);
        var icms = new ImpostoIcms
        {
            Origem = dto.Imposto.Origem,
            Cst = ehCsosn ? string.Empty : dto.Imposto.CstCsosn,
            Csosn = ehCsosn ? dto.Imposto.CstCsosn : string.Empty,
            BaseCalculo = dto.Imposto.BaseCalculoIcms,
            Aliquota = dto.Imposto.AliquotaIcms,
            PercentualReducaoBc = dto.Imposto.PercentualReducaoBc,
            BaseCalculoSt = dto.Imposto.BaseCalculoIcmsSt,
            AliquotaSt = dto.Imposto.AliquotaIcmsSt,
            PercentualMvaSt = dto.Imposto.PercentualMvaSt,
            PercentualCredSN = dto.Imposto.PercentualCredSN
        };

        // Calcula valor ICMS para regime normal
        if (!ehCsosn && icms.BaseCalculo.HasValue && icms.Aliquota.HasValue)
        {
            var bc = icms.PercentualReducaoBc.HasValue
                ? Math.Round(icms.BaseCalculo.Value * (1 - icms.PercentualReducaoBc.Value / 100), 2)
                : icms.BaseCalculo.Value;
            icms.Valor = Math.Round(bc * icms.Aliquota.Value / 100, 2);
        }

        // Calcula ICMS-ST
        if (icms.BaseCalculoSt.HasValue && icms.AliquotaSt.HasValue)
            icms.ValorSt = Math.Round(icms.BaseCalculoSt.Value * icms.AliquotaSt.Value / 100, 2);

        // Calcula crédito Simples Nacional
        if (ehCsosn && icms.BaseCalculo.HasValue && icms.PercentualCredSN.HasValue)
            icms.ValorCredIcmsSN = Math.Round(icms.BaseCalculo.Value * icms.PercentualCredSN.Value / 100, 2);

        return new Produto
        {
            NumeroItem = numeroItem,
            Codigo = dto.Codigo,
            Descricao = dto.Descricao,
            Ncm = dto.Ncm,
            Cfop = dto.Cfop,
            UnidadeComercial = dto.Unidade,
            QuantidadeComercial = dto.Quantidade,
            ValorUnitarioComercial = dto.ValorUnitario,
            ValorTotal = valorTotal,
            UnidadeTributavel = dto.Unidade,
            QuantidadeTributavel = dto.Quantidade,
            ValorUnitarioTributavel = dto.ValorUnitario,
            ValorDesconto = dto.ValorDesconto == 0 ? null : dto.ValorDesconto,
            Imposto = new Imposto
            {
                Icms = icms,
                Pis = new ImpostoPis { Cst = dto.Imposto.CstPis, BaseCalculo = 0, Aliquota = 0, Valor = 0 },
                Cofins = new ImpostoCofins { Cst = dto.Imposto.CstCofins, BaseCalculo = 0, Aliquota = 0, Valor = 0 }
            }
        };
    }

    private static Totais CalcularTotais(List<Produto> produtos) => new()
    {
        IcmsTotais = new TotaisIcms
        {
            ValorProdutos = produtos.Sum(p => p.ValorTotal),
            ValorNf = produtos.Sum(p => p.ValorTotal),
            BaseCalculoIcms = produtos.Sum(p => p.Imposto.Icms.BaseCalculo ?? 0),
            ValorIcms = produtos.Sum(p => p.Imposto.Icms.Valor ?? 0),
            ValorDesconto = produtos.Sum(p => p.ValorDesconto ?? 0)
        }
    };

    private static Pagamento MapPagamento(List<PagamentoDtoRequest> pagamentos, decimal valorNf)
    {
        if (pagamentos.Count == 0)
            return new Pagamento
            {
                Detalhamentos = [new DetalhamentoPagamento { TipoPagamento = "90", ValorPagamento = valorNf }]
            };

        return new Pagamento
        {
            Detalhamentos = pagamentos.Select(p => new DetalhamentoPagamento
            {
                TipoPagamento = p.TipoPagamento,
                ValorPagamento = p.ValorPagamento,
                IndicadorPagamento = p.IndicadorPagamento
            }).ToList()
        };
    }

    private static int GerarCodigoNumerico()
    {
        var rng = Random.Shared;
        return rng.Next(10000000, 99999999);
    }
}
