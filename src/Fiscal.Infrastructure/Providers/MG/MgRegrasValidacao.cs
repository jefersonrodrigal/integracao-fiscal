using Fiscal.Domain.Interfaces;

namespace Fiscal.Infrastructure.Providers.MG;

/// <summary>
/// Regras de validação específicas de MG.
/// Cada regra deve referenciar sua base legal / nota técnica.
/// Permite auditoria e atualização pontual sem impactar outras UFs.
/// </summary>
public sealed record MgInscricaoEstadualObrigatoria : RegraValidacaoEstadual
{
    public override string Codigo => "MG-001";
    public override string Descricao => "Inscrição Estadual do emitente é obrigatória para operações internas em MG.";
    public override string ReferenciaLegal => "RICMS-MG Decreto 43.080/2002, Art. 65. Verificar atualização via SEFAZ-MG.";
}

public sealed record MgCfopInterestadualRestritoNFCe : RegraValidacaoEstadual
{
    public override string Codigo => "MG-002";
    public override string Descricao => "NFC-e em MG não admite CFOP interestadual (5xxx, 7xxx vedados).";
    public override string ReferenciaLegal => "NT 2021.004 ENCAT e Portaria SEFAZ-MG sobre NFC-e.";
}

public sealed record MgIeDestinatarioNaoContribuinte : RegraValidacaoEstadual
{
    public override string Codigo => "MG-003";
    public override string Descricao => "NFC-e emitida para consumidor final — destinatário deve ter indIEDest=9.";
    public override string ReferenciaLegal => "MOC NF-e 7.0 item 3.6.4.";
}
