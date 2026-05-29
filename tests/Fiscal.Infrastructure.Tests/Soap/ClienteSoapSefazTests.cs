using System.Net;
using System.Net.Http;
using FluentAssertions;
using Fiscal.Infrastructure.Soap;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fiscal.Infrastructure.Tests.Soap;

public sealed class ClienteSoapSefazTests
{
    private static IHttpClientFactory CriarFactoryComResposta(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(body, status);
        var client = new HttpClient(handler) { BaseAddress = null };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Sefaz").Returns(client);
        return factory;
    }

    [Fact]
    public async Task EnviarAsync_deve_retornar_sucesso_com_resposta_200()
    {
        var xmlResposta = "<retEnviNFe><cStat>100</cStat><xMotivo>Autorizado</xMotivo></retEnviNFe>";
        var factory = CriarFactoryComResposta(xmlResposta);
        var cliente = new ClienteSoapSefaz(factory, NullLogger<ClienteSoapSefaz>.Instance);

        var result = await cliente.EnviarAsync("<envelope/>", "http://fake/endpoint", "fakeSoapAction");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("retEnviNFe");
    }

    [Fact]
    public async Task EnviarAsync_deve_retornar_falha_com_resposta_500()
    {
        var factory = CriarFactoryComResposta("Internal Server Error", HttpStatusCode.InternalServerError);
        var cliente = new ClienteSoapSefaz(factory, NullLogger<ClienteSoapSefaz>.Instance);

        var result = await cliente.EnviarAsync("<envelope/>", "http://fake/endpoint", "fakeSoapAction");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500");
    }

    private sealed class MockHttpMessageHandler(string responseBody, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "text/xml")
            };
            return Task.FromResult(response);
        }
    }
}
