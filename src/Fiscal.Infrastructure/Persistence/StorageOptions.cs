namespace Fiscal.Infrastructure.Persistence;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string ProtocolosPath { get; set; } = Path.Combine("data", "protocolos");
    public string XmlAutorizadoPath { get; set; } = Path.Combine("data", "xml");
    public string DanfePath { get; set; } = Path.Combine("data", "danfe");
}
