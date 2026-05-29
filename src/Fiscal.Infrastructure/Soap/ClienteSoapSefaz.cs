using System.Net.Http.Headers;
using System.Text;
using Fiscal.Domain.Common;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fiscal.Infrastructure.Soap;

/// <summary>
/// Cliente HTTP/SOAP para comunicação com webservices SEFAZ.
/// Utiliza HttpClientFactory para gerenciamento correto de conexões.
/// Certificado mTLS deve ser configurado no HttpClientHandler (ver DI).
/// Referência: Manual de Integração NF-e v7.0, seção 3 (Comunicação).
/// </summary>
public sealed class ClienteSoapSefaz(
    IHttpClientFactory httpClientFactory,
    ILogger<ClienteSoapSefaz> logger) : IClienteSoapSefaz
{
    public async Task<Result<string>> EnviarAsync(
        string xmlEnvelope,
        string endpoint,
        string soapAction,
        CancellationToken ct = default)
    {
        logger.LogInformation("Enviando SOAP para {Endpoint} Action={Action}", endpoint, soapAction);

        try
        {
            using var client = httpClientFactory.CreateClient("Sefaz");

            var content = new StringContent(xmlEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", soapAction);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

            using var response = await client.SendAsync(request, ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SEFAZ retornou HTTP {Status}: {Body}", response.StatusCode, body[..Math.Min(500, body.Length)]);
                return Result<string>.Failure($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            logger.LogDebug("Resposta SEFAZ recebida ({Bytes} bytes)", body.Length);
            return Result<string>.Success(body);
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Timeout na comunicação com SEFAZ endpoint {Endpoint}", endpoint);
            return Result<string>.Failure("Timeout na comunicação com a SEFAZ. Tente novamente ou verifique a contingência.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Erro de rede ao comunicar com SEFAZ");
            return Result<string>.Failure($"Erro de rede: {ex.Message}");
        }
    }
}
