using Fiscal.Domain.Common;
using Fiscal.Domain.Enums;

namespace Fiscal.Domain.Interfaces;

public interface IValidadorXsd
{
    Result Validar(string xml, TipoDocumentoFiscal tipo, string versaoLayout = "4.00");
}
