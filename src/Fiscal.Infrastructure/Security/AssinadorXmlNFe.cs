using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Fiscal.Domain.Common;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fiscal.Infrastructure.Security;

/// <summary>
/// Assina digitalmente o XML da NF-e usando certificado A1 (ICP-Brasil).
/// Algoritmo: RSA + SHA1 conforme especificação ENCAT/SEFAZ (XmlDsig).
/// Referência: Manual de Integração NF-e v7.0, seção 4.1.
/// ATENÇÃO: Nunca armazenar senha do certificado em texto plano.
///          Use DPAPI, Azure Key Vault, ou Secret Manager em produção.
/// </summary>
public sealed class AssinadorXmlNFe(
    IOptions<CertificadoOptions> options,
    ILogger<AssinadorXmlNFe> logger) : IAssinadorXml
{
    public Result<string> Assinar(string xmlSemAssinatura, string tagReferencia = "infNFe")
    {
        try
        {
            var cert = CarregarCertificado();
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xmlSemAssinatura);

            var elemento = doc.GetElementsByTagName(tagReferencia)[0] as XmlElement
                ?? throw new InvalidOperationException($"Tag '{tagReferencia}' não encontrada no XML.");

            var id = elemento.GetAttribute("Id");
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException($"Atributo 'Id' não encontrado em '{tagReferencia}'.");

            var privateKey = cert.GetRSAPrivateKey()
                ?? throw new InvalidOperationException("Certificado não possui chave privada RSA.");

            var signedXml = new SignedXml(doc)
            {
                SigningKey = privateKey
            };

            signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;

            var reference = new Reference($"#{id}");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();

            var xmlSignature = signedXml.GetXml();
            var nfe = doc.GetElementsByTagName("NFe")[0] as XmlElement
                ?? doc.DocumentElement!;
            nfe.AppendChild(doc.ImportNode(xmlSignature, true));

            return Result<string>.Success(doc.OuterXml);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao assinar XML da NF-e");
            return Result<string>.Failure($"Falha na assinatura digital: {ex.Message}");
        }
    }

    private X509Certificate2 CarregarCertificado()
    {
        var opts = options.Value;

        if (!string.IsNullOrEmpty(opts.Thumbprint))
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, opts.Thumbprint, validOnly: false);
            if (certs.Count == 0)
                throw new InvalidOperationException($"Certificado com thumbprint '{opts.Thumbprint}' não encontrado no repositório.");
            return certs[0];
        }

        if (!string.IsNullOrEmpty(opts.CaminhoArquivoPfx))
        {
            if (!File.Exists(opts.CaminhoArquivoPfx))
                throw new FileNotFoundException("Arquivo PFX não encontrado.", opts.CaminhoArquivoPfx);

            // A senha é lida de forma segura — nunca de texto plano em código
            var senha = opts.ObterSenha();
            return X509CertificateLoader.LoadPkcs12FromFile(
                opts.CaminhoArquivoPfx,
                senha,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        }

        throw new InvalidOperationException("Nenhuma fonte de certificado configurada (Thumbprint ou CaminhoArquivoPfx).");
    }
}
