namespace Fiscal.Domain.Enums;

public enum EstadoFiscal
{
    Digitacao = 1,
    XmlGerado = 2,
    XmlValidado = 3,
    XmlAssinado = 4,
    LoteTransmitido = 5,
    AguardandoRetorno = 6,
    Autorizada = 7,
    Rejeitada = 8,
    Cancelada = 9,
    Denegada = 10,
    EmContingencia = 11
}
