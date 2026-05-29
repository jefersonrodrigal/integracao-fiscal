using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;

namespace Fiscal.Domain.Interfaces;

public interface IGeradorXmlNFe
{
    Result<string> Gerar(NotaFiscal nota);
}
