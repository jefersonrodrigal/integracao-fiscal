using FluentAssertions;
using Fiscal.Application.DTOs;
using Fiscal.Application.UseCases;
using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Fiscal.Application.Services;
using Fiscal.Infrastructure.Providers;
using Fiscal.Infrastructure.Providers.MG;
using Fiscal.Infrastructure.Soap;
using Fiscal.Infrastructure.Xml;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fiscal.Integration.Tests;

/// <summary>
/// Testes de integração que exercitam o fluxo completo end-to-end
/// com mocks nos pontos externos (SEFAZ, assinatura, DANFE).
/// </summary>
public sealed class FluxoCompletoTests
{
    private static EmitirNFeRequest CriarRequestCompleto() => new()
    {
        Modelo = TipoDocumentoFiscal.NFe,
        Ambiente = AmbienteSefaz.Homologacao,
        Uf = UnidadeFederativa.MG,
        NaturezaOperacao = "Venda de mercadoria",
        Serie = 1,
        Numero = 100,
        FinalidadeEmissao = 1,
        IndicadorPresencaComprador = 1,
        Emitente = new EmitenteDtoRequest
        {
            Cnpj = "11222333000181",
            RazaoSocial = "Empresa Exemplo Comércio Ltda",
            NomeFantasia = "Loja Exemplo",
            InscricaoEstadual = "0629328440072",
            CnaeCode = "4711301",
            CodigoRegimeTributario = "3",
            Endereco = new EnderecoDtoRequest
            {
                Logradouro = "Avenida do Contorno",
                Numero = "1000",
                Bairro = "Funcionários",
                CodigoMunicipio = "3106200",
                NomeMunicipio = "Belo Horizonte",
                Uf = "MG",
                Cep = "30110090",
                Telefone = "3130000000"
            }
        },
        Destinatario = new DestinatarioDtoRequest
        {
            Cnpj = "07526557000100",
            NomeRazaoSocial = "Empresa Compradora Ltda",
            IndicadorIe = 1,
            InscricaoEstadual = "9876543210",
            Email = "comprador@exemplo.com.br",
            Endereco = new EnderecoDtoRequest
            {
                Logradouro = "Rua do Comprador",
                Numero = "200",
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
                Codigo = "PROD001",
                Descricao = "Notebook Processador Intel i7 16GB RAM",
                Ncm = "84713012",
                Cfop = "5102",
                Unidade = "UN",
                Quantidade = 2,
                ValorUnitario = 3500.00m,
                ValorDesconto = 0,
                Imposto = new ImpostoDtoRequest
                {
                    Origem = "0",
                    CstCsosn = "00",
                    BaseCalculoIcms = 7000m,
                    AliquotaIcms = 12m,
                    CstPis = "01",
                    CstCofins = "01"
                }
            }
        ],
        InformacoesAdicionais = "NF-e emitida em ambiente de homologação - SEM VALOR FISCAL"
    };

    private static EmitirNFeUseCase CriarUseCaseComMocks()
    {
        // Componentes reais
        var geradorXml = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var validadorXsd = Substitute.For<IValidadorXsd>();
        validadorXsd.Validar(Arg.Any<string>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<string>())
            .Returns(Result.Success());

        var assinador = Substitute.For<IAssinadorXml>();
        assinador.Assinar(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => Result<string>.Success(callInfo.ArgAt<string>(0)));

        var protocolo = new ProtocoloAutorizacao
        {
            NumeroProtocolo = "141240000099999",
            ChaveAcesso = string.Empty,
            CodigoStatus = 100,
            DescricaoStatus = "Autorizado o uso da NF-e",
            DataHoraRecebimento = DateTime.UtcNow,
            XmlProtocolo = "<protNFe/>"
        };

        var autorizador = Substitute.For<IAutorizadorNFe>();
        autorizador.AutorizarAsync(Arg.Any<NotaFiscal>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var nota = callInfo.ArgAt<NotaFiscal>(0);
                protocolo.ChaveAcesso = nota.ChaveAcesso?.Valor ?? string.Empty;
                var resultado = new ResultadoTransmissao
                {
                    CodigoStatus = 100,
                    Descricao = "Autorizado",
                    Resultados = [new ResultadoNota
                    {
                        ChaveAcesso = protocolo.ChaveAcesso,
                        CodigoStatus = 100,
                        Descricao = "Autorizado",
                        Protocolo = protocolo
                    }]
                };
                return Task.FromResult(Result<ResultadoTransmissao>.Success(resultado));
            });

        var repositorio = Substitute.For<IRepositorioProtocolo>();
        repositorio.SalvarAsync(Arg.Any<ProtocoloAutorizacao>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var registroXml = Substitute.For<IRegistroXmlAutorizado>();
        registroXml.SalvarXmlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var danfe = Substitute.For<IGeradorDanfe>();
        danfe.GerarAsync(Arg.Any<NotaFiscal>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<CancellationToken>())
            .Returns(Result<byte[]>.Success([0x25, 0x50, 0x44, 0x46])); // %PDF

        return new EmitirNFeUseCase(
            geradorXml, validadorXsd, assinador, autorizador,
            repositorio, registroXml, danfe,
            NullLogger<EmitirNFeUseCase>.Instance);
    }

    [Fact]
    public async Task FluxoCompleto_deve_gerar_xml_validar_assinar_e_autorizar()
    {
        var useCase = CriarUseCaseComMocks();
        var result = await useCase.ExecutarAsync(CriarRequestCompleto());

        result.IsSuccess.Should().BeTrue("o fluxo completo deve completar sem erros");
        result.Value!.Sucesso.Should().BeTrue();
        result.Value.ChaveAcesso.Should().NotBeNullOrEmpty();
        result.Value.ChaveAcesso!.Length.Should().Be(44, "chave de acesso deve ter 44 dígitos");
        result.Value.NumeroProtocolo.Should().Be("141240000099999");
        result.Value.Estado.Should().Be(EstadoFiscal.Autorizada);
        result.Value.DanfePdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FluxoCompleto_deve_gerar_xml_com_emitente_correto()
    {
        var geradorXml = new GeradorXmlNFe(NullLogger<GeradorXmlNFe>.Instance);
        var request = CriarRequestCompleto();

        // Criar entidade e gerar XML
        var nota = NotaFiscalFactory.Criar(request);
        var xmlResult = geradorXml.Gerar(nota);

        xmlResult.IsSuccess.Should().BeTrue();
        xmlResult.Value!.Should().Contain("11222333000181"); // CNPJ emitente
        xmlResult.Value!.Should().Contain("Empresa Exemplo Comércio Ltda");
        xmlResult.Value!.Should().Contain("3106200"); // código município BH
        xmlResult.Value!.Should().Contain("4.00"); // versão layout
    }

    [Fact]
    public async Task FluxoCompleto_MG_deve_usar_provedor_correto()
    {
        var provedor = new MgSefazProvider();
        var factory = new ProvedorSefazFactory([provedor]);

        factory.Suporta(UnidadeFederativa.MG).Should().BeTrue();
        var config = factory.Resolver(UnidadeFederativa.MG).ObterConfiguracao(AmbienteSefaz.Homologacao);

        config.EndpointAutorizacao.Should().Contain("hnfe");
        config.VersaoLayout.Should().Be("4.00");
    }
}
