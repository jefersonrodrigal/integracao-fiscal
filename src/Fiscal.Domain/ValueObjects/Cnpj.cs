namespace Fiscal.Domain.ValueObjects;

public sealed record Cnpj
{
    public string Valor { get; }

    public Cnpj(string valor)
    {
        var digits = new string(valor.Where(char.IsDigit).ToArray());
        if (!IsValid(digits))
            throw new ArgumentException($"CNPJ inválido: {valor}", nameof(valor));
        Valor = digits;
    }

    public string Formatado => $"{Valor[..2]}.{Valor[2..5]}.{Valor[5..8]}/{Valor[8..12]}-{Valor[12..]}";

    private static bool IsValid(string digits)
    {
        if (digits.Length != 14 || digits.Distinct().Count() == 1) return false;

        int sum = 0;
        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        for (int i = 0; i < 12; i++) sum += (digits[i] - '0') * weights1[i];
        int d1 = sum % 11 < 2 ? 0 : 11 - sum % 11;

        sum = 0;
        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        for (int i = 0; i < 13; i++) sum += (digits[i] - '0') * weights2[i];
        int d2 = sum % 11 < 2 ? 0 : 11 - sum % 11;

        return digits[12] - '0' == d1 && digits[13] - '0' == d2;
    }

    public override string ToString() => Valor;
}
