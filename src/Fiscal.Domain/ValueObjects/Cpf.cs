namespace Fiscal.Domain.ValueObjects;

public sealed record Cpf
{
    public string Valor { get; }

    public Cpf(string valor)
    {
        var digits = new string(valor.Where(char.IsDigit).ToArray());
        if (!IsValid(digits))
            throw new ArgumentException($"CPF inválido: {valor}", nameof(valor));
        Valor = digits;
    }

    public string Formatado => $"{Valor[..3]}.{Valor[3..6]}.{Valor[6..9]}-{Valor[9..]}";

    private static bool IsValid(string digits)
    {
        if (digits.Length != 11 || digits.Distinct().Count() == 1) return false;

        int sum = 0;
        for (int i = 0; i < 9; i++) sum += (digits[i] - '0') * (10 - i);
        int d1 = sum % 11 < 2 ? 0 : 11 - sum % 11;

        sum = 0;
        for (int i = 0; i < 10; i++) sum += (digits[i] - '0') * (11 - i);
        int d2 = sum % 11 < 2 ? 0 : 11 - sum % 11;

        return digits[9] - '0' == d1 && digits[10] - '0' == d2;
    }

    public override string ToString() => Valor;
}
