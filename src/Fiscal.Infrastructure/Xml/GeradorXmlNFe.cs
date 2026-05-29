using System.Globalization;
using System.Text;
using System.Xml;
using Fiscal.Domain.Common;
using Fiscal.Domain.Entities;
using Fiscal.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fiscal.Infrastructure.Xml;

/// <summary>
/// Gera o XML da NF-e/NFC-e conforme leiaute 4.00 (ENCAT / MOC 7.0).
/// Namespace obrigatório: http://www.portalfiscal.inf.br/nfe
/// </summary>
public sealed class GeradorXmlNFe(ILogger<GeradorXmlNFe> logger) : IGeradorXmlNFe
{
    private const string NsNFe = "http://www.portalfiscal.inf.br/nfe";

    public Result<string> Gerar(NotaFiscal nota)
    {
        try
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                OmitXmlDeclaration = false
            };

            using var writer = XmlWriter.Create(sb, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("NFe", NsNFe);
            EscreverInfNFe(writer, nota);
            writer.WriteEndElement(); // NFe
            writer.WriteEndDocument();
            writer.Flush();

            return Result<string>.Success(sb.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar XML da NF-e {Numero}", nota.Numero);
            return Result<string>.Failure($"Falha na geração do XML: {ex.Message}");
        }
    }

    private static void EscreverInfNFe(XmlWriter w, NotaFiscal nota)
    {
        w.WriteStartElement("infNFe");
        w.WriteAttributeString("Id", $"NFe{nota.ChaveAcesso?.Valor}");
        w.WriteAttributeString("versao", nota.VersaoLayout);

        EscreverIde(w, nota);
        EscreverEmitente(w, nota.Emitente);
        if (nota.Destinatario is not null)
            EscreverDestinatario(w, nota.Destinatario);
        EscreverDetalhamentos(w, nota.Produtos);
        EscreverTotais(w, nota.Totais);
        EscreverTransporte(w);
        EscreverPagamento(w, nota.Pagamento);
        EscreverInfAdic(w, nota);

        w.WriteEndElement(); // infNFe
    }

    private static void EscreverIde(XmlWriter w, NotaFiscal nota)
    {
        w.WriteStartElement("ide");
        w.WriteElementString("cUF", nota.CodigoUf.ToString());
        w.WriteElementString("cNF", nota.CodigoNumerico.ToString("D8"));
        w.WriteElementString("natOp", nota.NaturezaOperacao);
        w.WriteElementString("mod", ((int)nota.Modelo).ToString());
        w.WriteElementString("serie", nota.Serie.ToString());
        w.WriteElementString("nNF", nota.Numero.ToString());
        w.WriteElementString("dhEmi", nota.DataEmissao.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        if (nota.DataSaidaEntrada.HasValue)
            w.WriteElementString("dhSaiEnt", nota.DataSaidaEntrada.Value.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        w.WriteElementString("tpNF", nota.TipoNf.ToString());
        w.WriteElementString("idDest", nota.IndicadorDestinatario.ToString());
        w.WriteElementString("cMunFG", nota.Emitente.Endereco.CodigoMunicipio);
        w.WriteElementString("tpImp", nota.Modelo == Domain.Enums.TipoDocumentoFiscal.NFCe ? "4" : "1");
        w.WriteElementString("tpEmis", ((int)nota.TipoEmissao).ToString());
        w.WriteElementString("cDV", nota.ChaveAcesso?.Valor[^1..] ?? "0");
        w.WriteElementString("tpAmb", ((int)nota.Ambiente).ToString());
        w.WriteElementString("finNFe", nota.FinalidadeEmissao.ToString());
        w.WriteElementString("indFinal", nota.IndicadorPresencaComprador <= 1 ? "0" : "1");
        w.WriteElementString("indPres", nota.IndicadorPresencaComprador.ToString());
        w.WriteElementString("indIntermed", nota.IndicadorIntermediario.ToString());
        w.WriteElementString("procEmi", nota.ProcessoEmissao.ToString());
        w.WriteElementString("verProc", nota.VersaoProcesso.Length > 0 ? nota.VersaoProcesso : "1.0.0");
        w.WriteEndElement(); // ide
    }

    private static void EscreverEmitente(XmlWriter w, Emitente emit)
    {
        w.WriteStartElement("emit");
        w.WriteElementString("CNPJ", emit.Cnpj.Valor);
        w.WriteElementString("xNome", emit.RazaoSocial);
        if (!string.IsNullOrEmpty(emit.NomeFantasia))
            w.WriteElementString("xFant", emit.NomeFantasia);

        w.WriteStartElement("enderEmit");
        EscreverEndereco(w, emit.Endereco);
        w.WriteEndElement();

        w.WriteElementString("IE", emit.InscricaoEstadual);
        if (!string.IsNullOrEmpty(emit.InscricaoMunicipal))
        {
            w.WriteElementString("IM", emit.InscricaoMunicipal);
            if (!string.IsNullOrEmpty(emit.CnaeCode))
                w.WriteElementString("CNAE", emit.CnaeCode);
        }
        w.WriteElementString("CRT", emit.CodigoRegimeTributario);
        w.WriteEndElement(); // emit
    }

    private static void EscreverDestinatario(XmlWriter w, Destinatario dest)
    {
        w.WriteStartElement("dest");
        if (dest.Cnpj is not null)
            w.WriteElementString("CNPJ", dest.Cnpj.Valor);
        else if (dest.Cpf is not null)
            w.WriteElementString("CPF", dest.Cpf.Valor);
        else if (!string.IsNullOrEmpty(dest.IdEstrangeiro))
            w.WriteElementString("idEstrangeiro", dest.IdEstrangeiro);

        w.WriteElementString("xNome", dest.NomeRazaoSocial);

        if (dest.Endereco is not null)
        {
            w.WriteStartElement("enderDest");
            EscreverEndereco(w, dest.Endereco);
            w.WriteEndElement();
        }

        w.WriteElementString("indIEDest", dest.IndicadorIe.ToString());
        if (!string.IsNullOrEmpty(dest.InscricaoEstadual))
            w.WriteElementString("IE", dest.InscricaoEstadual);
        if (!string.IsNullOrEmpty(dest.Email))
            w.WriteElementString("email", dest.Email);
        w.WriteEndElement(); // dest
    }

    private static void EscreverEndereco(XmlWriter w, Endereco end)
    {
        w.WriteElementString("xLgr", end.Logradouro);
        w.WriteElementString("nro", end.Numero);
        if (!string.IsNullOrEmpty(end.Complemento))
            w.WriteElementString("xCpl", end.Complemento);
        w.WriteElementString("xBairro", end.Bairro);
        w.WriteElementString("cMun", end.CodigoMunicipio);
        w.WriteElementString("xMun", end.NomeMunicipio);
        w.WriteElementString("UF", end.Uf);
        w.WriteElementString("CEP", end.Cep);
        w.WriteElementString("cPais", end.CodigoPais);
        w.WriteElementString("xPais", end.NomePais);
        if (!string.IsNullOrEmpty(end.Telefone))
            w.WriteElementString("fone", end.Telefone);
    }

    private static void EscreverDetalhamentos(XmlWriter w, List<Produto> produtos)
    {
        foreach (var p in produtos)
        {
            w.WriteStartElement("det");
            w.WriteAttributeString("nItem", p.NumeroItem.ToString());

            // prod
            w.WriteStartElement("prod");
            w.WriteElementString("cProd", p.Codigo);
            w.WriteElementString("cEAN", p.Ean);
            w.WriteElementString("xProd", p.Descricao);
            w.WriteElementString("NCM", p.Ncm);
            w.WriteElementString("CFOP", p.Cfop);
            w.WriteElementString("uCom", p.UnidadeComercial);
            w.WriteElementString("qCom", p.QuantidadeComercial.ToString("0.####", CultureInfo.InvariantCulture));
            w.WriteElementString("vUnCom", p.ValorUnitarioComercial.ToString("0.##########", CultureInfo.InvariantCulture));
            w.WriteElementString("vProd", p.ValorTotal.ToString("F2", CultureInfo.InvariantCulture));
            w.WriteElementString("cEANTrib", p.EanTributavel);
            w.WriteElementString("uTrib", p.UnidadeTributavel);
            w.WriteElementString("qTrib", p.QuantidadeTributavel.ToString("0.####", CultureInfo.InvariantCulture));
            w.WriteElementString("vUnTrib", p.ValorUnitarioTributavel.ToString("0.##########", CultureInfo.InvariantCulture));
            if (p.ValorFrete.HasValue) w.WriteElementString("vFrete", p.ValorFrete.Value.ToString("F2", CultureInfo.InvariantCulture));
            if (p.ValorSeguro.HasValue) w.WriteElementString("vSeg", p.ValorSeguro.Value.ToString("F2", CultureInfo.InvariantCulture));
            if (p.ValorDesconto.HasValue) w.WriteElementString("vDesc", p.ValorDesconto.Value.ToString("F2", CultureInfo.InvariantCulture));
            if (p.ValorOutrasDespesas.HasValue) w.WriteElementString("vOutro", p.ValorOutrasDespesas.Value.ToString("F2", CultureInfo.InvariantCulture));
            w.WriteElementString("indTot", p.CompoeTotalNF ? "1" : "0");
            w.WriteEndElement(); // prod

            // imposto
            EscreverImposto(w, p.Imposto);
            w.WriteEndElement(); // det
        }
    }

    private static void EscreverImposto(XmlWriter w, Imposto imp)
    {
        w.WriteStartElement("imposto");
        if (imp.ValorTotalTributos.HasValue)
            w.WriteElementString("vTotTrib", imp.ValorTotalTributos.Value.ToString("F2", CultureInfo.InvariantCulture));

        w.WriteStartElement("ICMS");
        var icms = imp.Icms;
        if (!string.IsNullOrEmpty(icms.Csosn))
            EscreverIcmsSimplesNacional(w, icms);
        else
            EscreverIcmsNormal(w, icms);
        w.WriteEndElement(); // ICMS

        if (imp.Pis is not null)
        {
            w.WriteStartElement("PIS");
            EscreverGrupoPisCofinsCst(w, "PIS", "pPIS", imp.Pis.Cst, imp.Pis.BaseCalculo, imp.Pis.Aliquota, imp.Pis.Valor, imp.Pis.QuantidadeVendida, imp.Pis.AliquotaReais);
            w.WriteEndElement(); // PIS
        }

        if (imp.Cofins is not null)
        {
            w.WriteStartElement("COFINS");
            EscreverGrupoPisCofinsCst(w, "COFINS", "pCOFINS", imp.Cofins.Cst, imp.Cofins.BaseCalculo, imp.Cofins.Aliquota, imp.Cofins.Valor, imp.Cofins.QuantidadeVendida, imp.Cofins.AliquotaReais);
            w.WriteEndElement(); // COFINS
        }

        w.WriteEndElement(); // imposto
    }

    private static void EscreverGrupoPisCofinsCst(XmlWriter w, string tributo, string tagAliquota,
        string cst, decimal? bc, decimal? aliq, decimal? valor, decimal? qtd, decimal? aliqReais)
    {
        // CST 01/02 → Alíquota  |  CST 03 → Quantidade  |  CST 04-09/49/50/99 → NT/ST
        var grupo = cst switch
        {
            "01" or "02" => $"{tributo}Aliq",
            "03" => $"{tributo}Qtde",
            _ => $"{tributo}NT"
        };

        w.WriteStartElement(grupo);
        w.WriteElementString("CST", cst);
        if (grupo == $"{tributo}Aliq")
        {
            w.WriteElementString("vBC", (bc ?? 0).ToString("F2", CultureInfo.InvariantCulture));
            w.WriteElementString(tagAliquota, (aliq ?? 0).ToString("F4", CultureInfo.InvariantCulture));
            w.WriteElementString($"v{tributo}", (valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
        }
        else if (grupo == $"{tributo}Qtde")
        {
            w.WriteElementString("qBCProd", (qtd ?? 0).ToString("F4", CultureInfo.InvariantCulture));
            w.WriteElementString($"v{tributo}Unit", (aliqReais ?? 0).ToString("F4", CultureInfo.InvariantCulture));
            w.WriteElementString($"v{tributo}", (valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
        }
        w.WriteEndElement();
    }

    private static void EscreverIcmsNormal(XmlWriter w, ImpostoIcms icms)
    {
        switch (icms.Cst)
        {
            case "00":
                w.WriteStartElement("ICMS00");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                w.WriteElementString("modBC", icms.ModalidadeBc);
                w.WriteElementString("vBC", (icms.BaseCalculo ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMS", (icms.Aliquota ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMS", (icms.Valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "10":
                w.WriteStartElement("ICMS10");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                w.WriteElementString("modBC", icms.ModalidadeBc);
                w.WriteElementString("vBC", (icms.BaseCalculo ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMS", (icms.Aliquota ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMS", (icms.Valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("modBCST", icms.ModalidadeBcSt);
                if (icms.PercentualMvaSt.HasValue) w.WriteElementString("pMVAST", icms.PercentualMvaSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBcSt.HasValue) w.WriteElementString("pRedBCST", icms.PercentualReducaoBcSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCST", (icms.BaseCalculoSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMSST", (icms.AliquotaSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSST", (icms.ValorSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPST", (icms.BaseCalculoFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPST", (icms.PercentualFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPST", (icms.ValorFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "20":
                w.WriteStartElement("ICMS20");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                w.WriteElementString("modBC", icms.ModalidadeBc);
                if (icms.PercentualReducaoBc.HasValue) w.WriteElementString("pRedBC", icms.PercentualReducaoBc.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBC", (icms.BaseCalculo ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMS", (icms.Aliquota ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMS", (icms.Valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                EscreverDesoneracao(w, icms);
                w.WriteEndElement();
                break;

            case "30":
                w.WriteStartElement("ICMS30");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                w.WriteElementString("modBCST", icms.ModalidadeBcSt);
                if (icms.PercentualMvaSt.HasValue) w.WriteElementString("pMVAST", icms.PercentualMvaSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBcSt.HasValue) w.WriteElementString("pRedBCST", icms.PercentualReducaoBcSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCST", (icms.BaseCalculoSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMSST", (icms.AliquotaSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSST", (icms.ValorSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPST", (icms.BaseCalculoFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPST", (icms.PercentualFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPST", (icms.ValorFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                EscreverDesoneracao(w, icms);
                w.WriteEndElement();
                break;

            case "40":
            case "41":
            case "50":
                w.WriteStartElement("ICMS40");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                EscreverDesoneracao(w, icms);
                w.WriteEndElement();
                break;

            case "51":
                w.WriteStartElement("ICMS51");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                w.WriteElementString("modBC", icms.ModalidadeBc);
                if (icms.PercentualReducaoBc.HasValue) w.WriteElementString("pRedBC", icms.PercentualReducaoBc.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBC", (icms.BaseCalculo ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMS", (icms.Aliquota ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                if (icms.ValorIcmsOp.HasValue) w.WriteElementString("vICMSOp", icms.ValorIcmsOp.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualDiferimento.HasValue) w.WriteElementString("pDif", icms.PercentualDiferimento.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.ValorIcmsDiferido.HasValue) w.WriteElementString("vICMSDif", icms.ValorIcmsDiferido.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMS", (icms.Valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCP", (icms.BaseCalculoFcp ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCP", (icms.PercentualFcp ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCP", (icms.ValorFcp ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "60":
                w.WriteStartElement("ICMS60");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                w.WriteElementString("vBCSTRet", (icms.BaseCalculoStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pST", (icms.PercentualSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSSTRet", (icms.ValorIcmsStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPSTRet", (icms.BaseCalculoFcpStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPSTRet", (icms.PercentualFcpStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPSTRet", (icms.ValorFcpStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "70":
                w.WriteStartElement("ICMS70");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", icms.Cst);
                w.WriteElementString("modBC", icms.ModalidadeBc);
                if (icms.PercentualReducaoBc.HasValue) w.WriteElementString("pRedBC", icms.PercentualReducaoBc.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBC", (icms.BaseCalculo ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMS", (icms.Aliquota ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMS", (icms.Valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("modBCST", icms.ModalidadeBcSt);
                if (icms.PercentualMvaSt.HasValue) w.WriteElementString("pMVAST", icms.PercentualMvaSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBcSt.HasValue) w.WriteElementString("pRedBCST", icms.PercentualReducaoBcSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCST", (icms.BaseCalculoSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMSST", (icms.AliquotaSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSST", (icms.ValorSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPST", (icms.BaseCalculoFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPST", (icms.PercentualFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPST", (icms.ValorFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                EscreverDesoneracao(w, icms);
                w.WriteEndElement();
                break;

            default: // "90"
                w.WriteStartElement("ICMS90");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CST", string.IsNullOrEmpty(icms.Cst) ? "90" : icms.Cst);
                w.WriteElementString("modBC", icms.ModalidadeBc);
                w.WriteElementString("vBC", (icms.BaseCalculo ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBc.HasValue) w.WriteElementString("pRedBC", icms.PercentualReducaoBc.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMS", (icms.Aliquota ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMS", (icms.Valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("modBCST", icms.ModalidadeBcSt);
                if (icms.PercentualMvaSt.HasValue) w.WriteElementString("pMVAST", icms.PercentualMvaSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBcSt.HasValue) w.WriteElementString("pRedBCST", icms.PercentualReducaoBcSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCST", (icms.BaseCalculoSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMSST", (icms.AliquotaSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSST", (icms.ValorSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPST", (icms.BaseCalculoFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPST", (icms.PercentualFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPST", (icms.ValorFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                EscreverDesoneracao(w, icms);
                w.WriteEndElement();
                break;
        }
    }

    private static void EscreverDesoneracao(XmlWriter w, ImpostoIcms icms)
    {
        if (icms.ValorIcmsDesonerado.HasValue)
        {
            w.WriteElementString("vICMSDeson", icms.ValorIcmsDesonerado.Value.ToString("F2", CultureInfo.InvariantCulture));
            w.WriteElementString("motDesICMS", icms.MotivoDesoneracaoIcms ?? "9");
        }
    }

    private static void EscreverIcmsSimplesNacional(XmlWriter w, ImpostoIcms icms)
    {
        switch (icms.Csosn)
        {
            case "101":
                w.WriteStartElement("ICMSSN101");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CSOSN", icms.Csosn);
                w.WriteElementString("pCredSN", (icms.PercentualCredSN ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vCredICMSSN", (icms.ValorCredIcmsSN ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "102":
            case "103":
            case "300":
            case "400":
                w.WriteStartElement("ICMSSN102");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CSOSN", icms.Csosn);
                w.WriteEndElement();
                break;

            case "201":
                w.WriteStartElement("ICMSSN201");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CSOSN", icms.Csosn);
                w.WriteElementString("modBCST", icms.ModalidadeBcSt);
                if (icms.PercentualMvaSt.HasValue) w.WriteElementString("pMVAST", icms.PercentualMvaSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBcSt.HasValue) w.WriteElementString("pRedBCST", icms.PercentualReducaoBcSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCST", (icms.BaseCalculoSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMSST", (icms.AliquotaSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSST", (icms.ValorSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPST", (icms.BaseCalculoFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPST", (icms.PercentualFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPST", (icms.ValorFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pCredSN", (icms.PercentualCredSN ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vCredICMSSN", (icms.ValorCredIcmsSN ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "202":
            case "203":
                w.WriteStartElement("ICMSSN202");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CSOSN", icms.Csosn);
                w.WriteElementString("modBCST", icms.ModalidadeBcSt);
                if (icms.PercentualMvaSt.HasValue) w.WriteElementString("pMVAST", icms.PercentualMvaSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBcSt.HasValue) w.WriteElementString("pRedBCST", icms.PercentualReducaoBcSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCST", (icms.BaseCalculoSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMSST", (icms.AliquotaSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSST", (icms.ValorSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPST", (icms.BaseCalculoFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPST", (icms.PercentualFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPST", (icms.ValorFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "500":
                w.WriteStartElement("ICMSSN500");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CSOSN", icms.Csosn);
                w.WriteElementString("vBCSTRet", (icms.BaseCalculoStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pST", (icms.PercentualSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSSTRet", (icms.ValorIcmsStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPSTRet", (icms.BaseCalculoFcpStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPSTRet", (icms.PercentualFcpStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPSTRet", (icms.ValorFcpStRetido ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            case "900":
                w.WriteStartElement("ICMSSN900");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CSOSN", icms.Csosn);
                w.WriteElementString("modBC", icms.ModalidadeBc);
                w.WriteElementString("vBC", (icms.BaseCalculo ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBc.HasValue) w.WriteElementString("pRedBC", icms.PercentualReducaoBc.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMS", (icms.Aliquota ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMS", (icms.Valor ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("modBCST", icms.ModalidadeBcSt);
                if (icms.PercentualMvaSt.HasValue) w.WriteElementString("pMVAST", icms.PercentualMvaSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                if (icms.PercentualReducaoBcSt.HasValue) w.WriteElementString("pRedBCST", icms.PercentualReducaoBcSt.Value.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCST", (icms.BaseCalculoSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pICMSST", (icms.AliquotaSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vICMSST", (icms.ValorSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vBCFCPST", (icms.BaseCalculoFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pFCPST", (icms.PercentualFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vFCPST", (icms.ValorFcpSt ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("pCredSN", (icms.PercentualCredSN ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteElementString("vCredICMSSN", (icms.ValorCredIcmsSN ?? 0).ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
                break;

            default:
                w.WriteStartElement("ICMSSN102");
                w.WriteElementString("orig", icms.Origem);
                w.WriteElementString("CSOSN", icms.Csosn);
                w.WriteEndElement();
                break;
        }
    }

    private static void EscreverTotais(XmlWriter w, Totais totais)
    {
        var t = totais.IcmsTotais;
        w.WriteStartElement("total");
        w.WriteStartElement("ICMSTot");
        w.WriteElementString("vBC", t.BaseCalculoIcms.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vICMS", t.ValorIcms.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vICMSDeson", "0.00");
        w.WriteElementString("vFCPUFDest", "0.00");
        w.WriteElementString("vICMSUFDest", "0.00");
        w.WriteElementString("vICMSUFRemet", "0.00");
        w.WriteElementString("vFCP", "0.00");
        w.WriteElementString("vBCST", t.BaseCalculoIcmsSt.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vST", t.ValorIcmsSt.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vFCPST", t.ValorFcpSt.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vFCPSTRet", t.ValorFcpStRetido.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vProd", t.ValorProdutos.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vFrete", t.ValorFrete.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vSeg", t.ValorSeguro.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vDesc", t.ValorDesconto.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vII", t.ValorIi.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vIPI", t.ValorIpi.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vIPIDevol", t.ValorIpiDevolvido.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vPIS", t.ValorPis.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vCOFINS", t.ValorCofins.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vOutro", t.ValorOutrasDespesas.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vNF", t.ValorNf.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteElementString("vTotTrib", t.ValorTotalTributos.ToString("F2", CultureInfo.InvariantCulture));
        w.WriteEndElement(); // ICMSTot
        w.WriteEndElement(); // total
    }

    private static void EscreverPagamento(XmlWriter w, Pagamento pag)
    {
        w.WriteStartElement("pag");
        foreach (var det in pag.Detalhamentos)
        {
            w.WriteStartElement("detPag");
            if (det.IndicadorPagamento is not null)
                w.WriteElementString("indPag", det.IndicadorPagamento);
            w.WriteElementString("tPag", det.TipoPagamento);
            w.WriteElementString("vPag", det.ValorPagamento.ToString("F2", CultureInfo.InvariantCulture));
            w.WriteEndElement(); // detPag
        }
        w.WriteEndElement(); // pag
    }

    private static void EscreverTransporte(XmlWriter w)
    {
        w.WriteStartElement("transp");
        w.WriteElementString("modFrete", "9"); // 9=Sem frete
        w.WriteEndElement();
    }

    private static void EscreverInfAdic(XmlWriter w, NotaFiscal nota)
    {
        if (string.IsNullOrEmpty(nota.InformacoesAdicionaisFisco) &&
            string.IsNullOrEmpty(nota.InformacoesAdicionaisContribuinte))
            return;

        w.WriteStartElement("infAdic");
        if (!string.IsNullOrEmpty(nota.InformacoesAdicionaisFisco))
            w.WriteElementString("infAdFisco", nota.InformacoesAdicionaisFisco);
        if (!string.IsNullOrEmpty(nota.InformacoesAdicionaisContribuinte))
            w.WriteElementString("infCpl", nota.InformacoesAdicionaisContribuinte);
        w.WriteEndElement();
    }
}
