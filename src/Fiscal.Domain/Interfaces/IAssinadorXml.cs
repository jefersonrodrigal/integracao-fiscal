using Fiscal.Domain.Common;

namespace Fiscal.Domain.Interfaces;

public interface IAssinadorXml
{
    Result<string> Assinar(string xmlSemAssinatura, string tagReferencia = "infNFe");
}
