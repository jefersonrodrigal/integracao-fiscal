namespace Fiscal.Domain.ValueObjects;

/// <summary>
/// Chave de acesso de 44 dígitos da NF-e/NFC-e conforme leiaute ENCAT/SEFAZ.
/// cUF(2) + AAMM(4) + CNPJ(14) + mod(2) + serie(3) + nNF(9) + tpEmis(1) + cNF(8) + cDV(1)
/// </summary>
public sealed record ChaveAcesso
{
    public string Valor { get; }

    private ChaveAcesso(string valor) => Valor = valor;

    public static ChaveAcesso Criar(
        int cUF, int anoMes, string cnpj, int modelo,
        int serie, int numero, int tipoEmissao, int codigoNumerico)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        var base44 = $"{cUF:D2}{anoMes:D4}{digits}{modelo:D2}{serie:D3}{numero:D9}{tipoEmissao}{codigoNumerico:D8}";
        int dv = CalcularDv(base44);
        return new ChaveAcesso($"{base44}{dv}");
    }

    public static ChaveAcesso FromString(string chave)
    {
        var digits = new string(chave.Where(char.IsDigit).ToArray());
        if (digits.Length != 44)
            throw new ArgumentException("Chave de acesso deve ter 44 dígitos.", nameof(chave));
        return new ChaveAcesso(digits);
    }

    private static int CalcularDv(string chave44SemDv)
    {
        // Módulo 11 com pesos 2-9 da direita para esquerda (especificação ENCAT)
        int sum = 0;
        int peso = 2;
        for (int i = chave44SemDv.Length - 1; i >= 0; i--)
        {
            sum += (chave44SemDv[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        int resto = sum % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Valor;
}
