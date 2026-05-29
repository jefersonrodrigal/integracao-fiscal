namespace Fiscal.Infrastructure.Security;

/// <summary>
/// Configurações do certificado digital A1 para assinatura de NF-e.
/// JAMAIS armazene a senha em appsettings.json ou em texto plano.
/// Use: DPAPI (Windows), Azure Key Vault, AWS Secrets Manager, ou variáveis de ambiente protegidas.
/// </summary>
public sealed class CertificadoOptions
{
    public const string SectionName = "Certificado";

    /// <summary>
    /// Thumbprint do certificado instalado no repositório do sistema (preferencial em produção).
    /// </summary>
    public string? Thumbprint { get; set; }

    /// <summary>
    /// Caminho absoluto para o arquivo .pfx (alternativa ao Thumbprint).
    /// </summary>
    public string? CaminhoArquivoPfx { get; set; }

    /// <summary>
    /// Nome da variável de ambiente que contém a senha do certificado.
    /// Nunca salvar a senha diretamente aqui.
    /// </summary>
    public string SenhaEnvVar { get; set; } = "FISCAL_CERT_SENHA";

    /// <summary>
    /// Obtém a senha do certificado da variável de ambiente configurada.
    /// Em produção substitua por leitura de secret store.
    /// </summary>
    public string ObterSenha()
    {
        var senha = Environment.GetEnvironmentVariable(SenhaEnvVar);
        if (string.IsNullOrEmpty(senha))
            throw new InvalidOperationException(
                $"Senha do certificado não encontrada na variável de ambiente '{SenhaEnvVar}'. " +
                "Configure a variável de ambiente ou use um secret store seguro.");
        return senha;
    }
}
