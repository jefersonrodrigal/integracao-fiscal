using FluentAssertions;
using Fiscal.Application.DTOs;
using Fiscal.Application.Services;
using Fiscal.Domain.Enums;

namespace Fiscal.Application.Tests.Services;

public sealed class NotaFiscalFactoryTests
{
    private static EmitirNFeRequest CriarRequestValido() => new()
    {
        Modelo = TipoDocumentoFiscal.NFe,
        Ambiente = AmbienteSefaz.Homologacao,
        Uf = UnidadeFederativa.MG,
        NaturezaOperacao = "Venda de mercadoria",
        Serie = 1,
        Numero = 1,
        Emitente = new EmitenteDtoRequest
        {
            Cnpj = "11222333000181",
            RazaoSocial = "Empresa Teste Ltda",
            InscricaoEstadual = "1234567890",
            CnaeCode = "4711301",
            CodigoRegimeTributario = "1",
            Endereco = new EnderecoDtoRequest
            {
                Logradouro = "Rua Teste",
                Numero = "100",
                Bairro = "Centro",
                CodigoMunicipio = "3106200",
                NomeMunicipio = "Belo Horizonte",
                Uf = "MG",
                Cep = "30130010"
            }
        },
        Produtos =
        [
            new ProdutoDtoRequest
            {
                Codigo = "001",
                Descricao = "Produto Teste",
                Ncm = "84713012",
                Cfop = "5102",
                Unidade = "UN",
                Quantidade = 1,
                ValorUnitario = 100.00m,
                Imposto = new ImpostoDtoRequest
                {
                    Origem = "0",
                    CstCsosn = "00",
                    BaseCalculoIcms = 100m,
                    AliquotaIcms = 12m,
                    CstPis = "07",
                    CstCofins = "07"
                }
            }
        ]
    };

    [Fact]
    public void Criar_deve_gerar_nota_fiscal_com_campos_basicos_preenchidos()
    {
        var nota = NotaFiscalFactory.Criar(CriarRequestValido());

        nota.Should().NotBeNull();
        nota.Modelo.Should().Be(TipoDocumentoFiscal.NFe);
        nota.Ambiente.Should().Be(AmbienteSefaz.Homologacao);
        nota.Uf.Should().Be(UnidadeFederativa.MG);
        nota.NaturezaOperacao.Should().Be("Venda de mercadoria");
        nota.Serie.Should().Be(1);
        nota.Numero.Should().Be(1);
    }

    [Fact]
    public void Criar_deve_gerar_chave_de_acesso_44_digitos()
    {
        var nota = NotaFiscalFactory.Criar(CriarRequestValido());

        nota.ChaveAcesso.Should().NotBeNull();
        nota.ChaveAcesso!.Valor.Should().HaveLength(44);
    }

    [Fact]
    public void Criar_deve_mapear_emitente_corretamente()
    {
        var nota = NotaFiscalFactory.Criar(CriarRequestValido());

        nota.Emitente.Cnpj.Valor.Should().Be("11222333000181");
        nota.Emitente.RazaoSocial.Should().Be("Empresa Teste Ltda");
        nota.Emitente.Endereco.Uf.Should().Be("MG");
    }

    [Fact]
    public void Criar_deve_calcular_totais_dos_produtos()
    {
        var nota = NotaFiscalFactory.Criar(CriarRequestValido());

        nota.Totais.IcmsTotais.ValorNf.Should().Be(100m);
        nota.Totais.IcmsTotais.ValorProdutos.Should().Be(100m);
    }

    [Fact]
    public void Criar_deve_numerar_itens_sequencialmente()
    {
        var request = CriarRequestValido();
        request.Produtos.Add(new ProdutoDtoRequest
        {
            Codigo = "002", Descricao = "Produto 2", Ncm = "84713012",
            Cfop = "5102", Unidade = "UN", Quantidade = 1, ValorUnitario = 50m,
            Imposto = new ImpostoDtoRequest { CstCsosn = "00" }
        });

        var nota = NotaFiscalFactory.Criar(request);

        nota.Produtos[0].NumeroItem.Should().Be(1);
        nota.Produtos[1].NumeroItem.Should().Be(2);
    }
}
