using System.Reflection;
using System.Text.Json.Serialization;
using Fiscal.Api.Middleware;
using Fiscal.Application.UseCases;
using Fiscal.Infrastructure.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "md-fiscal",
        Version = "v1",
        Description =
            """
            # Plataforma Fiscal Eletrônica
            Documentação oficial da API **md-fiscal** para emissão, transmissão e gestão de documentos fiscais eletrônicos.

            ---
            ## Documentos suportados
            | Documento | Modelo | Uso |
            |---|---:|---|
            | **NF-e** | 55 | Nota Fiscal Eletrônica |
            | **NFC-e** | 65 | Nota Fiscal de Consumidor Eletrônica |

            ## Fluxo de autorização
            | Etapa | Processo |
            |---:|---|
            | 1 | Geração do XML fiscal |
            | 2 | Validação contra XSD |
            | 3 | Assinatura digital |
            | 4 | Transmissão para SEFAZ |
            | 5 | Retorno do protocolo de autorização |

            ## Endpoints principais
            | Método | Endpoint | Descrição |
            |---|---|---|
            | `POST` | `/api/v1/nfe/emitir` | Emite NF-e/NFC-e |
            | `GET` | `/api/v1/nfe/recibo/{nRec}` | Consulta recibo de lote |
            | `GET` | `/api/v1/nfe/status-servico` | Consulta status da SEFAZ |

            ## Ambientes
            | Ambiente | Finalidade |
            |---|---|
            | `Producao` | Documentos fiscais reais |
            | `Homologacao` | Testes sem valor fiscal |

            ## Conformidade
            - Leiaute NF-e **4.00**
            - ENCAT / MOC 7.0
            - SEFAZ-MG
            - Certificado digital ICP-Brasil
            - Assinatura digital **RSA + SHA1**

            ## © Direitos
            Desenvolvido por **Jeferson Almeida** | **NextLevel Soluções**  
            Todos os direitos reservados.
            """,
        Contact = new OpenApiContact
        {
            Name = "Portal NF-e ENCAT",
            Url = new Uri("https://www.nfe.fazenda.gov.br")
        },
        License = new OpenApiLicense
        {
            Name = "Uso conforme legislação fiscal vigente"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.OrderActionsBy(api =>
    {
        var controller = api.ActionDescriptor.RouteValues["controller"];
        return $"{controller}_{api.RelativePath}_{api.HttpMethod}";
    });

    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

builder.Services.AddFiscalInfrastructure(builder.Configuration);

builder.Services.AddScoped<EmitirNFeUseCase>();
builder.Services.AddScoped<ConsultarReciboUseCase>();
builder.Services.AddScoped<ConsultarStatusServicoUseCase>();

builder.Services.AddLogging();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger(options =>
{
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "docs";
    options.DocumentTitle = "md-fiscal | Documentação da API";

    options.SwaggerEndpoint("/swagger/v1/swagger.json", "md-fiscal API v1");

    options.DocExpansion(DocExpansion.None);

    options.DefaultModelsExpandDepth(-1);
    options.DefaultModelExpandDepth(2);

    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnablePersistAuthorization();

    options.ShowExtensions();
    options.ShowCommonExtensions();

    options.EnableTryItOutByDefault();

    options.DisplayOperationId();
});

app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();