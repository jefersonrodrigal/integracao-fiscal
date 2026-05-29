using System.Xml;
using System.Xml.Schema;
using Fiscal.Domain.Common;
using Fiscal.Domain.Enums;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fiscal.Infrastructure.Xml;

/// <summary>
/// Valida XML de NF-e/NFC-e contra schemas XSD oficiais.
/// Os XSDs devem estar em: [BaseDirectory]/Schemas/NFe/{versao}/
/// Fonte: Portal NF-e (www.nfe.fazenda.gov.br) — baixar schemas atualizados conforme NT vigente.
/// </summary>
public sealed class ValidadorXsd(ILogger<ValidadorXsd> logger) : IValidadorXsd
{
    private static readonly Dictionary<string, XmlSchemaSet> _cache = [];
    private static readonly Lock _lock = new();

    public Result Validar(string xml, TipoDocumentoFiscal tipo, string versaoLayout = "4.00")
    {
        var erros = new List<string>();

        try
        {
            var schemaSet = ObterSchemaSet(tipo, versaoLayout);

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema
                                | XmlSchemaValidationFlags.ReportValidationWarnings
            };

            settings.ValidationEventHandler += (_, e) =>
            {
                if (e.Severity == XmlSeverityType.Error)
                    erros.Add($"[XSD Error] {e.Message}");
                else
                    logger.LogDebug("XSD Warning: {Msg}", e.Message);
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read()) { }
        }
        catch (XmlSchemaValidationException ex)
        {
            erros.Add($"[XSD Fatal] {ex.Message}");
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Schema XSD não encontrado para {Tipo} versão {Versao}", tipo, versaoLayout);
            return Result.Failure($"Schema XSD não encontrado: {ex.Message}. Certifique-se de que os schemas oficiais estão em Schemas/NFe/{versaoLayout}/");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado na validação XSD");
            return Result.Failure($"Erro na validação XSD: {ex.Message}");
        }

        return erros.Count > 0 ? Result.Failure(erros) : Result.Success();
    }

    private static XmlSchemaSet ObterSchemaSet(TipoDocumentoFiscal tipo, string versao)
    {
        var cacheKey = $"{tipo}-{versao}";

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schemas", "NFe", versao);
            var schemaFile = tipo == TipoDocumentoFiscal.NFe ? "nfe_v4.00.xsd" : "nfce_v4.00.xsd";
            var fullPath = Path.Combine(schemaPath, schemaFile);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Schema XSD não encontrado: {fullPath}", fullPath);

            var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
            set.Add("http://www.portalfiscal.inf.br/nfe", new Uri(fullPath).AbsoluteUri);
            set.Compile();

            _cache[cacheKey] = set;
            return set;
        }
    }
}
