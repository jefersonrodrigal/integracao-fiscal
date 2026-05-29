using System.Xml;
using FluentAssertions;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.ValueObjects;
using Fiscal.Infrastructure.Xml;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fiscal.Infrastructure.Tests.Xml;

public sealed class GeradorXmlNFeTests
{
    private static NotaFiscal CriarNotaValida()
    {
        var nota = new NotaFiscal
        {
            Modelo = TipoDocumentoFiscal.NFe,
            Ambiente = AmbienteSefaz.Homologacao,
            Uf = UnidadeFederativa.MG,
            CodigoUf = 31,
            NaturezaOperacao = "Venda de mercadoria",
            Serie = 1,
            Numero = 1,
            DataEmissao = new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            TipoNf = 1,
            IndicadorDestinatario = 1,
            CodigoNumerico = 12345678,
            VersaoLayout = "4.00",
            FinalidadeEmissao = 1,
            IndicadorPresencaComprador = 1,
            Emitente = new Emitente
            {
                Cnpj = new Cnpj("11222333000181"),
                RazaoSocial = "Empresa Teste Ltda",
                InscricaoEstadual = "1234567890",
                CnaeCode = "4711301",
                CodigoRegimeTributario = "1",
                Endereco = new Endereco
                {
                    Logradouro = "Rua Teste",
                    Numero = "100",
                    Bairro = "Centro",
                    CodigoMunicipio = "3106200",
                    NomeMunicipio = "Belo Horizonte",
                    Uf = "MG",
                    Cep = "30130010"
                }
            }
        };

        nota.ChaveAcesso = ChaveAcesso.Criar(31, 2405, "11222333000181", 55, 1, 1, 1, 12345678);

        nota.Produtos.Add(new Produto
        {
            NumeroItem = 1,
            Codigo = "001",
            Descricao = "Produto Teste",
            Ncm = "84713012",
            Cfop = "5102",
            UnidadeComercial = "UN",
            QuantidadeComercial = 1,
            ValorUnitarioComercial = 100m,
            ValorTotal = 100m,
            UnidadeTributavel = "UN",
            QuantidadeTributavel = 1,
            ValorUnitarioTributavel = 100m,
            Imposto = new Imposto
            {
                Icms = new ImpostoIcms { Origem = "0", Cst = "00", ModalidadeBc = "3", BaseCalculo = 100m, Aliquota = 12m, Valor = 12m },
                Pis = new ImpostoPis { Cst = "07", BaseCalculo = 0, Aliquota = 0, Valor = 0 },
                Cofins = new ImpostoCofins { Cst = "07", BaseCalculo = 0, Aliquota = 0, Valor = 0 }
            }
        });

        nota.Totais = new Totais
        {
            IcmsTotais = new TotaisIcms
            {
                ValorProdutos = 100m, ValorNf = 100m,
                BaseCalculoIcms = 100m, ValorIcms = 12m
            }
        };

        return nota;
    }

    [Fact]
    public void Gerar_deve_retornar_xml_valido()
    {
        var gerador = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var nota = CriarNotaValida();

        var result = gerador.Gerar(nota);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Gerar_deve_produzir_xml_parseavel()
    {
        var gerador = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var result = gerador.Gerar(CriarNotaValida());

        var doc = new XmlDocument();
        var act = () => doc.LoadXml(result.Value!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Gerar_deve_conter_elemento_NFe_no_namespace_correto()
    {
        var gerador = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var result = gerador.Gerar(CriarNotaValida());

        var doc = new XmlDocument();
        doc.LoadXml(result.Value!);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

        var nfeElement = doc.SelectSingleNode("/nfe:NFe", ns);
        nfeElement.Should().NotBeNull();
    }

    [Fact]
    public void Gerar_deve_conter_chave_de_acesso_no_atributo_Id()
    {
        var gerador = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var nota = CriarNotaValida();
        var result = gerador.Gerar(nota);

        result.Value!.Should().Contain($"Id=\"NFe{nota.ChaveAcesso!.Valor}\"");
    }

    [Fact]
    public void Gerar_deve_conter_CNPJ_emitente()
    {
        var gerador = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var result = gerador.Gerar(CriarNotaValida());

        result.Value!.Should().Contain("<CNPJ>11222333000181</CNPJ>");
    }

    [Fact]
    public void Gerar_deve_conter_versao_layout_400()
    {
        var gerador = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var result = gerador.Gerar(CriarNotaValida());

        result.Value!.Should().Contain("versao=\"4.00\"");
    }
}
