using FluentAssertions;
using Fiscal.Application.DTOs;
using Fiscal.Application.UseCases;
using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fiscal.Application.Tests.UseCases;

public sealed class EmitirNFeUseCaseTests
{
    private readonly IGeradorXmlNFe _geradorXml = Substitute.For<IGeradorXmlNFe>();
    private readonly IValidadorXsd _validadorXsd = Substitute.For<IValidadorXsd>();
    private readonly IAssinadorXml _assinadorXml = Substitute.For<IAssinadorXml>();
    private readonly IAutorizadorNFe _autorizadorNFe = Substitute.For<IAutorizadorNFe>();
    private readonly IRepositorioProtocolo _repositorio = Substitute.For<IRepositorioProtocolo>();
    private readonly IRegistroXmlAutorizado _registroXml = Substitute.For<IRegistroXmlAutorizado>();
    private readonly IGeradorDanfe _geradorDanfe = Substitute.For<IGeradorDanfe>();

    private EmitirNFeUseCase CriarUseCase() => new(
        _geradorXml, _validadorXsd, _assinadorXml, _autorizadorNFe,
        _repositorio, _registroXml, _geradorDanfe,
        NullLogger<EmitirNFeUseCase>.Instance);

    private static EmitirNFeRequest CriarRequest() => new()
    {
        Modelo = TipoDocumentoFiscal.NFe,
        Ambiente = AmbienteSefaz.Homologacao,
        Uf = UnidadeFederativa.MG,
        NaturezaOperacao = "Venda",
        Serie = 1,
        Numero = 1,
        Emitente = new EmitenteDtoRequest
        {
            Cnpj = "11222333000181",
            RazaoSocial = "Empresa Teste",
            InscricaoEstadual = "1234567890",
            CnaeCode = "4711301",
            CodigoRegimeTributario = "1",
            Endereco = new EnderecoDtoRequest
            {
                Logradouro = "Rua A", Numero = "1", Bairro = "Centro",
                CodigoMunicipio = "3106200", NomeMunicipio = "BH", Uf = "MG", Cep = "30130010"
            }
        },
        Produtos =
        [
            new ProdutoDtoRequest
            {
                Codigo = "001", Descricao = "Prod", Ncm = "84713012",
                Cfop = "5102", Unidade = "UN", Quantidade = 1, ValorUnitario = 100m,
                Imposto = new ImpostoDtoRequest { CstCsosn = "00", BaseCalculoIcms = 100m, AliquotaIcms = 12m, CstPis = "07", CstCofins = "07" }
            }
        ]
    };

    [Fact]
    public async Task ExecutarAsync_deve_retornar_sucesso_com_protocolo()
    {
        var protocolo = new ProtocoloAutorizacao
        {
            NumeroProtocolo = "141240000001234",
            ChaveAcesso = "31240511222333000181550010000000011123456785",
            CodigoStatus = 100,
            DescricaoStatus = "Autorizado o uso da NF-e",
            DataHoraRecebimento = DateTime.UtcNow,
            XmlProtocolo = "<protNFe/>"
        };

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

        _geradorXml.Gerar(Arg.Any<NotaFiscal>()).Returns(Result<string>.Success("<NFe/>"));
        _validadorXsd.Validar(Arg.Any<string>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<string>()).Returns(Result.Success());
        _assinadorXml.Assinar(Arg.Any<string>(), Arg.Any<string>()).Returns(Result<string>.Success("<NFeAssinado/>"));
        _autorizadorNFe.AutorizarAsync(Arg.Any<NotaFiscal>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResultadoTransmissao>.Success(resultado));
        _repositorio.SalvarAsync(Arg.Any<ProtocoloAutorizacao>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _registroXml.SalvarXmlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _geradorDanfe.GerarAsync(Arg.Any<NotaFiscal>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<CancellationToken>())
            .Returns(Result<byte[]>.Success([0x25, 0x50, 0x44, 0x46]));

        var uc = CriarUseCase();
        var result = await uc.ExecutarAsync(CriarRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sucesso.Should().BeTrue();
        result.Value.NumeroProtocolo.Should().Be("141240000001234");
        result.Value.Estado.Should().Be(EstadoFiscal.Autorizada);
    }

    [Fact]
    public async Task ExecutarAsync_deve_retornar_falha_quando_xml_invalido()
    {
        _geradorXml.Gerar(Arg.Any<NotaFiscal>()).Returns(Result<string>.Success("<NFe/>"));
        _validadorXsd.Validar(Arg.Any<string>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<string>())
            .Returns(Result.Failure("Campo obrigatório ausente"));

        var uc = CriarUseCase();
        var result = await uc.ExecutarAsync(CriarRequest());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Campo obrigatório ausente");
    }

    [Fact]
    public async Task ExecutarAsync_deve_retornar_falha_quando_assinatura_falha()
    {
        _geradorXml.Gerar(Arg.Any<NotaFiscal>()).Returns(Result<string>.Success("<NFe/>"));
        _validadorXsd.Validar(Arg.Any<string>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<string>()).Returns(Result.Success());
        _assinadorXml.Assinar(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<string>.Failure("Certificado inválido"));

        var uc = CriarUseCase();
        var result = await uc.ExecutarAsync(CriarRequest());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Certificado inválido");
    }

    [Fact]
    public async Task ExecutarAsync_deve_retornar_rejeicao_quando_sefaz_rejeita()
    {
        var resultado = new ResultadoTransmissao
        {
            CodigoStatus = 225,
            Descricao = "Rejeição: Código do município do emitente inválido",
            Resultados = []
        };

        _geradorXml.Gerar(Arg.Any<NotaFiscal>()).Returns(Result<string>.Success("<NFe/>"));
        _validadorXsd.Validar(Arg.Any<string>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<string>()).Returns(Result.Success());
        _assinadorXml.Assinar(Arg.Any<string>(), Arg.Any<string>()).Returns(Result<string>.Success("<NFeAssinado/>"));
        _autorizadorNFe.AutorizarAsync(Arg.Any<NotaFiscal>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResultadoTransmissao>.Success(resultado));

        var uc = CriarUseCase();
        var result = await uc.ExecutarAsync(CriarRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sucesso.Should().BeFalse();
        result.Value.CodigoStatus.Should().Be(225);
    }
}
