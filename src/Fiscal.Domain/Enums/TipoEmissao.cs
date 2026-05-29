namespace Fiscal.Domain.Enums;

/// <summary>
/// Forma de emissão da NF-e conforme Manual de Orientação do Contribuinte (MOC).
/// </summary>
public enum TipoEmissao
{
    Normal = 1,
    ContingenciaFSIA = 2,
    ContingenciaScaner = 3,
    ContingenciaDPEC = 4,
    ContingenciaFSDA = 5,
    ContingenciaSVCAN = 6,
    ContingenciaSVCRS = 7,
    ContingenciaOfflineNFCe = 9
}
