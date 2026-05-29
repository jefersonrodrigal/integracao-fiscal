using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Fiscal.Domain.Interfaces;
using Fiscal.Infrastructure.Danfe;
using Fiscal.Infrastructure.Persistence;
using Fiscal.Infrastructure.Providers;
using Fiscal.Infrastructure.Providers.MG;
using Fiscal.Infrastructure.Security;
using Fiscal.Infrastructure.Soap;
using Fiscal.Infrastructure.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fiscal.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddFiscalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.Configure<CertificadoOptions>(configuration.GetSection(CertificadoOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        // Provedores estaduais — adicione novos estados aqui sem alterar lógica
        services.AddSingleton<IProvedorSefazPorEstado, MgSefazProvider>();
        // services.AddSingleton<IProvedorSefazPorEstado, SpSefazProvider>();
        // services.AddSingleton<IProvedorSefazPorEstado, RjSefazProvider>();

        services.AddSingleton<ProvedorSefazFactory>();

        // XML
        services.AddScoped<IGeradorXmlNFe, GeradorXmlNFe>();
        services.AddScoped<IValidadorXsd, ValidadorXsd>();

        // Segurança
        services.AddScoped<IAssinadorXml, AssinadorXmlNFe>();

        // SOAP
        services.AddScoped<IClienteSoapSefaz, ClienteSoapSefaz>();
        services.AddScoped<IConsultadorRecibo, ConsultadorRecibo>();
        services.AddScoped<IConsultadorStatusServico, ConsultadorStatusServico>();
        services.AddScoped<IAutorizadorNFe, AutorizadorNFe>();

        // Persistência
        services.AddScoped<IRepositorioProtocolo, RepositorioProtocoloArquivo>();
        services.AddScoped<IRegistroXmlAutorizado, RegistroXmlAutorizadoArquivo>();

        // DANFE
        services.AddScoped<IGeradorDanfe, GeradorDanfePlaceholder>();

        // HttpClient com mTLS para SEFAZ
        services.AddHttpClient("Sefaz", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            // Em produção: carregar certificado e adicionar ao handler
            // handler.ClientCertificates.Add(cert);
            // handler.ServerCertificateCustomValidationCallback para aceitar CA SEFAZ
            return handler;
        });

        return services;
    }
}
