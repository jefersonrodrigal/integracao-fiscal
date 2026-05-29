# md-fiscal — Plataforma Fiscal Eletrônica Brasileira

Plataforma para emissão, transmissão, autorização e gestão de **NF-e** (modelo 55) e **NFC-e** (modelo 65) no padrão ENCAT / SEFAZ, desenvolvida em **.NET 10** com **Clean Architecture**.

Implementação inicial para **Minas Gerais (MG)**, com arquitetura extensível para todas as UFs.

---

## Sumário

- [Visão Geral](#visão-geral)
- [Status](#status)
- [Tecnologias](#tecnologias)
- [Arquitetura](#arquitetura)
- [Estrutura de Pastas](#estrutura-de-pastas)
- [Modelos de Domínio](#modelos-de-domínio)
- [Interfaces Principais](#interfaces-principais)
- [Implementação MG](#implementação-mg)
- [Fluxo Completo de Autorização](#fluxo-completo-de-autorização)
- [Testes](#testes)
- [Como Executar](#como-executar)
- [Configuração](#configuração)
- [Expandindo para Novos Estados](#expandindo-para-novos-estados)
- [Conformidade Fiscal](#conformidade-fiscal)
- [Segurança](#segurança)
- [Checklist de Produção](#checklist-de-produção)
- [Cuidados Legais e Técnicos](#cuidados-legais-e-técnicos)

---

## Visão Geral

O **md-fiscal** implementa o ciclo completo de vida de uma nota fiscal eletrônica:

| # | Etapa | Implementação |
|---|-------|---------------|
| 1 | Geração do XML da NF-e/NFC-e | `GeradorXmlNFe` — leiaute 4.00 ENCAT |
| 2 | Validação contra XSD oficial | `ValidadorXsd` — schemas do Portal NF-e |
| 3 | Assinatura digital ICP-Brasil | `AssinadorXmlNFe` — RSA + SHA1 + XMLDsig |
| 4 | Transmissão via SOAP para SEFAZ | `ClienteSoapSefaz` — SOAP 1.2 com mTLS |
| 5 | Envio do lote | `AutorizadorNFe` — enviNFe síncrono e assíncrono |
| 6 | Consulta de recibo | `ConsultadorRecibo` — polling nRec |
| 7 | Recebimento da autorização | Parser de retEnviNFe / retConsReciNFe |
| 8 | Salvamento do protocolo | `RepositorioProtocoloArquivo` (extensível para BD) |
| 9 | Geração de DANFE / DANFCE | `IGeradorDanfe` (ponto de extensão para PDF) |
| 10 | Isolamento de regras por UF | `IProvedorSefazPorEstado` + Strategy Pattern |

---

## Status

```
Build:   OK — 0 erros, 0 warnings
Testes:  59 passando, 0 falhando, 0 ignorados
Runtime: .NET 10.0.300
```

---

## Tecnologias

| Categoria | Pacote / Framework |
|-----------|-------------------|
| Runtime | .NET 10 |
| Web API | ASP.NET Core 10 (Minimal Hosting) |
| XML | `System.Xml`, `System.Xml.Schema` |
| Assinatura Digital | `System.Security.Cryptography.Xml` 10.0.8 |
| Certificado X.509 | `System.Security.Cryptography.X509Certificates` |
| HTTP / SOAP | `HttpClientFactory` + `Microsoft.Extensions.Http` |
| Logging | `Microsoft.Extensions.Logging.Abstractions` |
| Injeção de Dependência | `Microsoft.Extensions.DependencyInjection` |
| Testes | xUnit + FluentAssertions 7.0 + NSubstitute 5.3 |

---

## Arquitetura

O projeto segue **Clean Architecture** com dependência unidirecional de fora para dentro:

```
┌──────────────────────────────────────────────────────┐
│                     Fiscal.Api                       │
│   NFeController · GlobalExceptionMiddleware          │
│   Program.cs (DI, middleware, roteamento)            │
└──────────────────────┬───────────────────────────────┘
                       │ depende de
┌──────────────────────▼───────────────────────────────┐
│                 Fiscal.Application                   │
│   EmitirNFeUseCase · ConsultarReciboUseCase          │
│   NotaFiscalFactory · DTOs (Request / Response)      │
└────────────┬─────────────────────────┬───────────────┘
             │ depende de              │ injeta via interfaces
┌────────────▼──────────┐   ┌─────────▼──────────────────────┐
│    Fiscal.Domain      │   │      Fiscal.Infrastructure      │
│                       │   │                                 │
│  Entidades            │   │  Xml/GeradorXmlNFe              │
│  Value Objects        │   │  Xml/ValidadorXsd               │
│  Interfaces           │   │  Security/AssinadorXmlNFe       │
│  Enums                │   │  Soap/ClienteSoapSefaz          │
│  Result Pattern       │   │  Soap/AutorizadorNFe            │
│                       │   │  Soap/ConsultadorRecibo         │
│  (zero dependências   │   │  Soap/EnvelopeSoapBuilder       │
│   externas)           │   │  Soap/RetornoSefazParser        │
│                       │   │  Providers/MG/*                 │
│                       │   │  Providers/ProvedorSefazFactory │
│                       │   │  Persistence/*                  │
│                       │   │  Danfe/GeradorDanfePlaceholder  │
└───────────────────────┘   └─────────────────────────────────┘
```

### Princípios Aplicados

- **SOLID** — interfaces segregadas, injeção de dependência, aberto para extensão
- **DRY** — lógica de estado (Result Pattern) centralizada, sem duplicação de parsing
- **KISS** — cada classe tem uma única responsabilidade bem definida
- **Strategy Pattern** — resolução de provedor SEFAZ por UF via `ProvedorSefazFactory`
- **Factory Pattern** — `NotaFiscalFactory` cria entidades a partir de DTOs
- **Result Pattern** — sem exceções para fluxo de negócio; `Result<T>` em todas as operações

---

## Estrutura de Pastas

```
md-fiscal/
├── src/
│   ├── Fiscal.Domain/
│   │   ├── Common/
│   │   │   └── Result.cs                    # Result<T> e Result sem genérico
│   │   ├── Enums/
│   │   │   ├── AmbienteSefaz.cs             # Producao = 1, Homologacao = 2
│   │   │   ├── EstadoFiscal.cs              # Máquina de estados da NF-e
│   │   │   ├── TipoDocumentoFiscal.cs       # NFe = 55, NFCe = 65
│   │   │   ├── TipoEmissao.cs               # Normal, Contingência SVC-AN etc.
│   │   │   └── UnidadeFederativa.cs         # Todos os códigos IBGE das UFs
│   │   ├── ValueObjects/
│   │   │   ├── ChaveAcesso.cs               # 44 dígitos — cálculo dígito verificador mod11
│   │   │   ├── Cnpj.cs                      # Validação completa dos dígitos verificadores
│   │   │   └── Cpf.cs                       # Validação completa dos dígitos verificadores
│   │   ├── Entities/
│   │   │   ├── NotaFiscal.cs                # Raiz de agregado — máquina de estados
│   │   │   ├── Emitente.cs
│   │   │   ├── Destinatario.cs
│   │   │   ├── Produto.cs
│   │   │   ├── Imposto.cs                   # ICMS, PIS, COFINS, IPI
│   │   │   ├── Totais.cs                    # ICMSTot
│   │   │   ├── Lote.cs                      # enviNFe — agrupa notas para transmissão
│   │   │   ├── Recibo.cs                    # nRec retornado pela SEFAZ
│   │   │   ├── ProtocoloAutorizacao.cs      # nProt — prova de autorização
│   │   │   ├── ResultadoTransmissao.cs      # Resultado do lote + por nota
│   │   │   └── Endereco.cs
│   │   └── Interfaces/
│   │       ├── IGeradorXmlNFe.cs
│   │       ├── IValidadorXsd.cs
│   │       ├── IAssinadorXml.cs
│   │       ├── IClienteSoapSefaz.cs
│   │       ├── ITransmissorLote.cs
│   │       ├── IConsultadorRecibo.cs
│   │       ├── IAutorizadorNFe.cs
│   │       ├── IRepositorioProtocolo.cs
│   │       ├── IGeradorDanfe.cs
│   │       ├── IProvedorSefazPorEstado.cs   # + IConfiguracaoSefazProvider + RegraValidacaoEstadual
│   │       └── IRegistroXmlAutorizado.cs
│   │
│   ├── Fiscal.Application/
│   │   ├── DTOs/
│   │   │   ├── EmitirNFeRequest.cs          # Entrada da API — emitente, destinatário, produtos
│   │   │   └── EmitirNFeResponse.cs         # Saída — chave, protocolo, estado, PDF
│   │   ├── Services/
│   │   │   └── NotaFiscalFactory.cs         # DTO → entidade NotaFiscal + cálculo de chave
│   │   └── UseCases/
│   │       ├── EmitirNFeUseCase.cs          # Orquestra todas as 9 etapas
│   │       └── ConsultarReciboUseCase.cs    # Polling de recibo + persistência
│   │
│   ├── Fiscal.Infrastructure/
│   │   ├── Providers/
│   │   │   ├── MG/
│   │   │   │   ├── MgSefazProvider.cs       # IProvedorSefazPorEstado para MG
│   │   │   │   ├── MgConfiguracaoSefaz.cs   # IConfiguracaoSefazProvider — endpoints + SoapActions
│   │   │   │   ├── MgEndpointsSefaz.cs      # URLs oficiais SEFAZ-MG hom/prod
│   │   │   │   └── MgRegrasValidacao.cs     # MG-001, MG-002, MG-003 com referência legal
│   │   │   └── ProvedorSefazFactory.cs      # Resolve IProvedorSefazPorEstado por UF
│   │   ├── Xml/
│   │   │   ├── GeradorXmlNFe.cs             # XmlWriter — leiaute 4.00 completo
│   │   │   └── ValidadorXsd.cs              # XmlSchemaSet com cache thread-safe
│   │   ├── Security/
│   │   │   ├── AssinadorXmlNFe.cs           # RSA+SHA1 XMLDsig — cert A1 ICP-Brasil
│   │   │   └── CertificadoOptions.cs        # Thumbprint ou PFX — senha via env var
│   │   ├── Soap/
│   │   │   ├── ClienteSoapSefaz.cs          # HttpClientFactory — POST SOAP 1.2
│   │   │   ├── EnvelopeSoapBuilder.cs       # Monta enviNFe, consReciNFe, consStatServ
│   │   │   ├── RetornoSefazParser.cs        # Parse de retEnviNFe / protNFe
│   │   │   ├── AutorizadorNFe.cs            # Orquestra lote + polling
│   │   │   └── ConsultadorRecibo.cs         # Consulta nRec na SEFAZ
│   │   ├── Persistence/
│   │   │   ├── RepositorioProtocoloArquivo.cs  # Protocolo em JSON (extensível para BD)
│   │   │   ├── RegistroXmlAutorizadoArquivo.cs # nfeProc em arquivo
│   │   │   └── StorageOptions.cs
│   │   ├── Danfe/
│   │   │   └── GeradorDanfePlaceholder.cs   # Ponto de extensão — substitua por PDF real
│   │   └── Extensions/
│   │       └── InfrastructureServiceExtensions.cs  # Registro completo de DI
│   │
│   └── Fiscal.Api/
│       ├── Controllers/
│       │   └── NFeController.cs             # POST /api/v1/nfe/emitir, GET /recibo/{nRec}
│       ├── Middleware/
│       │   └── GlobalExceptionMiddleware.cs # Tratamento centralizado de exceções
│       └── Program.cs                       # Minimal hosting + DI + middleware
│
└── tests/
    ├── Fiscal.Domain.Tests/          (15 testes)
    │   ├── Common/ResultTests.cs
    │   └── ValueObjects/
    │       ├── ChaveAcessoTests.cs
    │       └── CnpjTests.cs
    ├── Fiscal.Application.Tests/     (9 testes)
    │   ├── Services/NotaFiscalFactoryTests.cs
    │   └── UseCases/EmitirNFeUseCaseTests.cs
    ├── Fiscal.Infrastructure.Tests/  (32 testes)
    │   ├── Xml/GeradorXmlNFeTests.cs
    │   ├── Soap/EnvelopeSoapBuilderTests.cs
    │   ├── Soap/ClienteSoapSefazTests.cs
    │   ├── Soap/RetornoSefazParserTests.cs
    │   ├── Providers/MgSefazProviderTests.cs
    │   ├── Providers/ProvedorSefazFactoryTests.cs
    │   └── Persistence/RepositorioProtocoloArquivoTests.cs
    └── Fiscal.Integration.Tests/     (3 testes)
        └── FluxoCompletoTests.cs
```

---

## Modelos de Domínio

### Entidades

| Entidade | Responsabilidade |
|----------|-----------------|
| `NotaFiscal` | Raiz de agregado. Contém a máquina de estados (`EstadoFiscal`) e todos os dados da NF-e/NFC-e |
| `Emitente` | Dados do emitente com `Cnpj` (Value Object validado), endereço e regime tributário |
| `Destinatario` | Pessoa física (`Cpf`), jurídica (`Cnpj`) ou estrangeiro. Indicador de IE |
| `Produto` | Detalhamento por item: código, NCM, CFOP, quantidades e valores |
| `Imposto` | ICMS (todos os grupos CST/CSOSN), PIS, COFINS, IPI |
| `Totais` | `ICMSTot` com todos os campos do leiaute 4.00 |
| `Lote` | Agrupa notas para transmissão em bloco (`enviNFe`) |
| `Recibo` | `nRec` retornado pela SEFAZ no envio assíncrono |
| `ProtocoloAutorizacao` | `nProt` — número, data, digest, status 100 |
| `ResultadoTransmissao` | Status do lote + lista de `ResultadoNota` por chave de acesso |

### Value Objects

| Value Object | Regra |
|-------------|-------|
| `ChaveAcesso` | 44 dígitos. Composição: cUF+AAMM+CNPJ+mod+serie+nNF+tpEmis+cNF+cDV. Dígito verificador por módulo 11 com pesos 2-9 (especificação ENCAT) |
| `Cnpj` | Valida os dois dígitos verificadores. Suporta entrada com ou sem máscara |
| `Cpf` | Valida os dois dígitos verificadores. Suporta entrada com ou sem máscara |

### Enums

| Enum | Valores |
|------|---------|
| `TipoDocumentoFiscal` | `NFe = 55`, `NFCe = 65` |
| `AmbienteSefaz` | `Producao = 1`, `Homologacao = 2` |
| `EstadoFiscal` | `Digitacao → XmlGerado → XmlValidado → XmlAssinado → LoteTransmitido → AguardandoRetorno → Autorizada / Rejeitada / Cancelada / Denegada / EmContingencia` |
| `TipoEmissao` | `Normal = 1`, `ContingenciaFSIA = 2`, `ContingenciaSVCAN = 6`, `ContingenciaSVCRS = 7`, `ContingenciaOfflineNFCe = 9` |
| `UnidadeFederativa` | Todos os 27 estados com código IBGE (MG = 31, SP = 35, RJ = 33 ...) |

---

## Interfaces Principais

Todas as interfaces vivem em `Fiscal.Domain.Interfaces` — o Domain não conhece nenhuma implementação.

```csharp
// Geração de XML
IGeradorXmlNFe.Gerar(NotaFiscal) → Result<string>

// Validação XSD
IValidadorXsd.Validar(xml, tipo, versao) → Result

// Assinatura digital
IAssinadorXml.Assinar(xml, tagReferencia) → Result<string>

// Comunicação SOAP
IClienteSoapSefaz.EnviarAsync(envelope, endpoint, action) → Task<Result<string>>

// Autorização completa
IAutorizadorNFe.AutorizarAsync(nota) → Task<Result<ResultadoTransmissao>>
IAutorizadorNFe.AutorizarLoteAsync(lote) → Task<Result<ResultadoTransmissao>>

// Consulta de recibo
IConsultadorRecibo.ConsultarAsync(nRec, uf, ambiente) → Task<Result<ResultadoTransmissao>>

// Persistência
IRepositorioProtocolo.SalvarAsync(protocolo) → Task<Result>
IRepositorioProtocolo.ObterPorChaveAsync(chave) → Task<Result<ProtocoloAutorizacao>>

// Armazenamento de XML autorizado
IRegistroXmlAutorizado.SalvarXmlAsync(chave, xml) → Task<Result>

// DANFE / DANFCE
IGeradorDanfe.GerarAsync(nota, tipo) → Task<Result<byte[]>>

// Provedor por estado (Strategy)
IProvedorSefazPorEstado.Uf → UnidadeFederativa
IProvedorSefazPorEstado.ObterConfiguracao(ambiente) → IConfiguracaoSefazProvider
IProvedorSefazPorEstado.ObterRegrasValidacao() → IEnumerable<RegraValidacaoEstadual>
```

### Result Pattern

Todas as operações retornam `Result<T>` ou `Result` — sem exceções para fluxo de negócio:

```csharp
var result = geradorXml.Gerar(nota);

if (result.IsFailure)
    return Result<EmitirNFeResponse>.Failure(result.Error);

// ou com Match:
var saida = result.Match(
    xml => ProcessarXml(xml),
    erro => TratarErro(erro));
```

---

## Implementação MG

### Endpoints Oficiais

| Serviço | Homologação | Produção |
|---------|-------------|----------|
| Autorização | `hnfe.fazenda.mg.gov.br/nfe2/services/NFeAutorizacao4` | `nfe.fazenda.mg.gov.br/...` |
| Retorno Autorização | `.../NFeRetAutorizacao4` | `.../NFeRetAutorizacao4` |
| Consulta Protocolo | `.../NFeConsultaProtocolo4` | `.../NFeConsultaProtocolo4` |
| Status Serviço | `.../NFeStatusServico4` | `.../NFeStatusServico4` |
| Recepção Evento | `.../NFeRecepcaoEvento4` | `.../NFeRecepcaoEvento4` |

> Fonte: Portal NF-e SEFAZ-MG. Verificar atualização em notas técnicas ENCAT antes de cada release.

### Regras de Validação MG

| Código | Regra | Referência Legal |
|--------|-------|-----------------|
| MG-001 | IE do emitente obrigatória para operações internas | RICMS-MG Decreto 43.080/2002, Art. 65 |
| MG-002 | NFC-e não admite CFOP interestadual (5xxx, 7xxx) | NT 2021.004 ENCAT + Portaria SEFAZ-MG |
| MG-003 | Destinatário de NFC-e deve ter `indIEDest = 9` | MOC NF-e 7.0 item 3.6.4 |

### SoapActions (WSDL ENCAT NF-e 4.00)

```
Autorização:         .../wsdl/NFeAutorizacao4/nfeAutorizacaoLote
Retorno Autorização: .../wsdl/NFeRetAutorizacao4/nfeRetAutorizacaoLote
Consulta Protocolo:  .../wsdl/NFeConsultaProtocolo4/nfeConsultaNF
Status Serviço:      .../wsdl/NFeStatusServico4/nfeStatusServicoNF
```

---

## Fluxo Completo de Autorização

```
POST /api/v1/nfe/emitir
         │
         ▼
  EmitirNFeUseCase
         │
         ├─ 1. NotaFiscalFactory.Criar(request)
         │        └─ Mapeia DTOs → entidades de domínio
         │           Calcula ChaveAcesso (44 dígitos, mod-11)
         │           Numera itens, calcula totais
         │
         ├─ 2. IGeradorXmlNFe.Gerar(nota)
         │        └─ XmlWriter — leiaute 4.00
         │           Namespace: http://www.portalfiscal.inf.br/nfe
         │           Elementos: ide, emit, dest, det[], total, transp, infAdic
         │           Estado → XmlGerado
         │
         ├─ 3. IValidadorXsd.Validar(xml)
         │        └─ XmlSchemaSet com cache
         │           Schemas: Schemas/NFe/4.00/nfe_v4.00.xsd
         │           Estado → XmlValidado
         │
         ├─ 4. IAssinadorXml.Assinar(xml)
         │        └─ SignedXml — RSA + SHA1
         │           Reference: #NFe{chave44}
         │           Transforms: EnvelopedSignature + C14N
         │           KeyInfo: KeyInfoX509Data (cert ICP-Brasil)
         │           Estado → XmlAssinado
         │
         ├─ 5. IAutorizadorNFe.AutorizarAsync(nota)
         │        └─ Monta enviNFe (XML do lote)
         │           Constrói envelope SOAP 1.2
         │           POST → SEFAZ (mTLS com cert A1)
         │           Parseia retEnviNFe
         │           status 103 → polling: GET nRec (aguarda 2s)
         │           status 100 → extrai protNFe
         │           Monta nfeProc (xmlAssinado + protNFe)
         │           Estado → Autorizada
         │
         ├─ 6. IRepositorioProtocolo.SalvarAsync(protocolo)
         │        └─ JSON: data/protocolos/{chave44}.json
         │
         ├─ 7. IRegistroXmlAutorizado.SalvarXmlAsync(chave, nfeProc)
         │        └─ XML: data/xml/{chave44}-nfe.xml
         │
         └─ 8. IGeradorDanfe.GerarAsync(nota)
                  └─ PDF: implementar biblioteca (ver seção DANFE)
                     Retorna byte[] → EmitirNFeResponse.DanfePdf
```

---

## Testes

### Cobertura por Projeto

| Projeto de Testes | Testes | Cobertura |
|-------------------|--------|-----------|
| `Fiscal.Domain.Tests` | 15 | Result Pattern, ChaveAcesso (dígito verificador), CNPJ (validação mod-11) |
| `Fiscal.Application.Tests` | 9 | NotaFiscalFactory (mapeamento, totais, numeração), EmitirNFeUseCase (sucesso, falha XSD, falha assinatura, rejeição SEFAZ) |
| `Fiscal.Infrastructure.Tests` | 32 | GeradorXmlNFe (estrutura XML, namespace, versão), ValidadorXsd, EnvelopeSoapBuilder, ClienteSoapSefaz (mock HTTP), RetornoSefazParser (autorização e rejeição), MgSefazProvider (endpoints, regras, SoapActions), ProvedorSefazFactory (resolução por UF), RepositorioProtocoloArquivo (CRUD em disco) |
| `Fiscal.Integration.Tests` | 3 | Fluxo end-to-end completo com mocks nos pontos externos |
| **Total** | **59** | **0 falhas** |

### Execução

```bash
# Todos os testes
dotnet test

# Por projeto
dotnet test tests/Fiscal.Domain.Tests
dotnet test tests/Fiscal.Application.Tests
dotnet test tests/Fiscal.Infrastructure.Tests
dotnet test tests/Fiscal.Integration.Tests

# Com detalhes
dotnet test --logger "console;verbosity=normal"
```

### Exemplo de Teste de Use Case (com NSubstitute)

```csharp
[Fact]
public async Task ExecutarAsync_deve_retornar_sucesso_com_protocolo()
{
    _geradorXml.Gerar(Arg.Any<NotaFiscal>())
        .Returns(Result<string>.Success("<NFe/>"));

    _validadorXsd.Validar(Arg.Any<string>(), Arg.Any<TipoDocumentoFiscal>(), Arg.Any<string>())
        .Returns(Result.Success());

    _assinadorXml.Assinar(Arg.Any<string>(), Arg.Any<string>())
        .Returns(Result<string>.Success("<NFeAssinado/>"));

    _autorizadorNFe.AutorizarAsync(Arg.Any<NotaFiscal>(), Arg.Any<CancellationToken>())
        .Returns(Result<ResultadoTransmissao>.Success(ResultadoAutorizado()));

    var result = await _useCase.ExecutarAsync(CriarRequest());

    result.IsSuccess.Should().BeTrue();
    result.Value!.NumeroProtocolo.Should().Be("141240000001234");
    result.Value.Estado.Should().Be(EstadoFiscal.Autorizada);
}
```

---

## Como Executar

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Certificado digital A1 ICP-Brasil (para transmissão real)
- Schemas XSD oficiais (para validação real)

### Clonar e Compilar

```bash
git clone <repositorio>
cd md-fiscal

dotnet restore
dotnet build
```

### Rodar a API

```bash
dotnet run --project src/Fiscal.Api
```

A API sobe em `https://localhost:5001` (ou porta configurada). Documentação OpenAPI disponível em `/openapi`.

### Endpoints Disponíveis

```
POST /api/v1/nfe/emitir
     Body: EmitirNFeRequest (JSON)
     Retorna: EmitirNFeResponse com chave, protocolo, estado, PDF

GET  /api/v1/nfe/recibo/{numeroRecibo}?uf=MG&ambiente=Homologacao
     Retorna: ResultadoTransmissao
```

### Exemplo de Requisição

```json
POST /api/v1/nfe/emitir
{
  "modelo": 55,
  "ambiente": 2,
  "uf": 31,
  "naturezaOperacao": "Venda de mercadoria",
  "serie": 1,
  "numero": 1,
  "emitente": {
    "cnpj": "11222333000181",
    "razaoSocial": "Empresa Teste Ltda",
    "inscricaoEstadual": "0629328440072",
    "cnaeCode": "4711301",
    "codigoRegimeTributario": "3",
    "endereco": {
      "logradouro": "Avenida do Contorno",
      "numero": "1000",
      "bairro": "Funcionários",
      "codigoMunicipio": "3106200",
      "nomeMunicipio": "Belo Horizonte",
      "uf": "MG",
      "cep": "30110090"
    }
  },
  "produtos": [
    {
      "codigo": "001",
      "descricao": "Notebook Intel i7",
      "ncm": "84713012",
      "cfop": "5102",
      "unidade": "UN",
      "quantidade": 1,
      "valorUnitario": 3500.00,
      "imposto": {
        "origem": "0",
        "cstCsosn": "00",
        "baseCalculoIcms": 3500.00,
        "aliquotaIcms": 12.0,
        "cstPis": "01",
        "cstCofins": "01"
      }
    }
  ]
}
```

---

## Configuração

### `appsettings.json`

```json
{
  "Certificado": {
    "Thumbprint": null,
    "CaminhoArquivoPfx": "C:\\Certificados\\empresa.pfx",
    "SenhaEnvVar": "FISCAL_CERT_SENHA"
  },
  "Storage": {
    "ProtocolosPath": "data/protocolos",
    "XmlAutorizadoPath": "data/xml",
    "DanfePath": "data/danfe"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Fiscal": "Debug"
    }
  }
}
```

### Variável de Ambiente (senha do certificado)

```bash
# Windows
setx FISCAL_CERT_SENHA "SenhaDoSeuCertificado"

# Linux / macOS
export FISCAL_CERT_SENHA="SenhaDoSeuCertificado"
```

> **NUNCA** coloque a senha do certificado em `appsettings.json`, no código ou em controle de versão.
> Em produção, use Azure Key Vault, AWS Secrets Manager ou DPAPI.

### Schemas XSD

Baixe os schemas oficiais do [Portal NF-e](https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=BMPFMBoln3w=) e coloque em:

```
src/Fiscal.Api/Schemas/NFe/4.00/
    nfe_v4.00.xsd
    nfce_v4.00.xsd
    (e todos os schemas auxiliares referenciados)
```

Adicione ao `.csproj` como `Content` com `CopyToOutputDirectory`:

```xml
<ItemGroup>
  <Content Include="Schemas\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

---

## Expandindo para Novos Estados

A arquitetura usa **Strategy Pattern** — adicionar uma nova UF não requer alterar nenhuma classe existente.

### Passo a Passo (exemplo: São Paulo)

**1. Criar o provider:**

```
src/Fiscal.Infrastructure/Providers/SP/
    SpSefazProvider.cs
    SpConfiguracaoSefaz.cs
    SpEndpointsSefaz.cs
    SpRegrasValidacao.cs
```

```csharp
// SpSefazProvider.cs
public sealed class SpSefazProvider : IProvedorSefazPorEstado
{
    public UnidadeFederativa Uf => UnidadeFederativa.SP;

    public IConfiguracaoSefazProvider ObterConfiguracao(AmbienteSefaz ambiente)
        => new SpConfiguracaoSefaz(ambiente);

    public IEnumerable<RegraValidacaoEstadual> ObterRegrasValidacao() =>
    [
        new SpDestaqueIcmsStObrigatorio(),
        // ...
    ];
}
```

**2. Registrar no DI:**

```csharp
// InfrastructureServiceExtensions.cs
services.AddSingleton<IProvedorSefazPorEstado, MgSefazProvider>();
services.AddSingleton<IProvedorSefazPorEstado, SpSefazProvider>(); // ← apenas esta linha
```

**3. Pronto.** `ProvedorSefazFactory` resolve automaticamente.

### UFs Planejadas

| UF | Código | Status |
|----|--------|--------|
| MG | 31 | ✅ Implementado |
| SP | 35 | Planejado |
| RJ | 33 | Planejado |
| PR | 41 | Planejado |
| BA | 29 | Planejado |
| RS | 43 | Planejado |
| Demais | — | Planejado |

---

## Conformidade Fiscal

### Referências Normativas

| Documento | Descrição |
|-----------|-----------|
| MOC NF-e 7.0 | Manual de Orientação do Contribuinte — leiaute, regras e processos |
| Leiaute NF-e 4.00 | Especificação técnica ENCAT dos campos XML |
| NT 2024.001 | Nota Técnica mais recente — verificar portal NF-e |
| NT 2021.004 | NFC-e — restrições de CFOP interestadual |
| NT 2013.006 | DANFCE — leiaute de impressão |
| RICMS-MG Decreto 43.080/2002 | Regulamento ICMS Minas Gerais |
| WSDL ENCAT NF-e 4.00 | Contratos dos webservices SEFAZ |

### Versionamento de Regras

Cada regra estadual em `MgRegrasValidacao.cs` (e nos futuros providers) contém:
- `Codigo` — identificador único por UF
- `Descricao` — regra em linguagem clara
- `ReferenciaLegal` — norma, artigo e orientação para atualização

Isso permite auditar, atualizar e versionar regras pontualmente sem reescrever a arquitetura.

---

## Segurança

| Aspecto | Implementação |
|---------|--------------|
| Senha do certificado | Lida de variável de ambiente — nunca em texto plano |
| Certificado em produção | Preferência por `Thumbprint` no repositório do sistema (`LocalMachine\My`) |
| Certificado A1 (PFX) | `X509CertificateLoader.LoadPkcs12FromFile` (API moderna .NET 10) |
| Comunicação SEFAZ | HTTPS + mTLS — certificado A1 no `HttpClientHandler` |
| Validação de entrada | CNPJ, CPF e ChaveAcesso validados por Value Objects com regras de negócio |
| XSD obrigatório | Toda NF-e é validada antes de assinar — impede transmissão de XML inválido |
| Logs | Nunca loga conteúdo de certificado ou senha — apenas thumbprint/chave/protocolo |

---

## Checklist de Produção

### Obrigatório antes de transmitir para produção

- [ ] **Schemas XSD** — baixar versão vigente do Portal NF-e e colocar em `Schemas/NFe/4.00/`
- [ ] **Certificado digital** — A1 ICP-Brasil válido, dentro da validade, emitido por AC credenciada ICP-Brasil
- [ ] **Senha do certificado** — configurada em variável de ambiente ou secret store seguro
- [ ] **mTLS** — configurar `HttpClientHandler` com o certificado A1 para autenticação mútua
- [ ] **Ambiente** — testar exaustivamente em homologação antes de mudar para `Producao = 1`
- [ ] **DANFE real** — substituir `GeradorDanfePlaceholder` por biblioteca PDF com leiaute oficial
- [ ] **Banco de dados** — substituir `RepositorioProtocoloArquivo` por EF Core + SQL Server/PostgreSQL
- [ ] **Contingência** — implementar fluxo SVC-AN e SVC-RS para indisponibilidade SEFAZ
- [ ] **Cancelamento e eventos** — implementar `NFeRecepcaoEvento4` para cancelamento, EPEC, CCe
- [ ] **Monitoramento** — logs estruturados, health check `/status`, alertas de rejeição SEFAZ
- [ ] **Atualização de NT** — monitorar Portal NF-e para novas Notas Técnicas (impactam schemas e regras)
- [ ] **Inutilização** — implementar `NFeInutilizacao4` para numeração de séries inutilizadas
- [ ] **Consulta cadastro** — `CadConsultaCadastro4` para validar IE do destinatário antes da emissão
- [ ] **Backup de XML** — manter cópia dos XMLs autorizados por no mínimo 5 anos (legislação fiscal)

---

## Cuidados Legais e Técnicos

### NF-e / NFC-e

- **XML autorizado tem valor jurídico** — o arquivo `nfeProc` (xmlAssinado + protNFe) é o documento fiscal. Guarde por 5 anos.
- **Chave de acesso** — identifica univocamente a nota. Nunca reuse numeração dentro da mesma série.
- **Rejeição ≠ erro** — status 2xx da SEFAZ indica comunicação bem-sucedida; o status fiscal vem no `cStat` do XML de retorno.
- **Contingência** — em caso de indisponibilidade SEFAZ, o contribuinte pode emitir em contingência (SVC-AN código 6, SVC-RS código 7); o sistema deve suportar essa transição sem perda de dados.
- **Cancelamento** — só é permitido dentro do prazo legal (em geral 24h após a autorização para NF-e, 30 minutos para NFC-e) e apenas se a mercadoria não circulou.
- **Versão do leiaute** — este projeto usa a versão **4.00** (vigente). Verificar se há versão mais recente conforme NT publicada pelo ENCAT.
- **Ambiente de homologação** — documentos emitidos em homologação (`tpAmb = 2`) **não têm valor fiscal**. Certifique-se de que o ambiente de produção usa `tpAmb = 1`.
- **DANFE** — o leiaute de impressão é regulamentado pelo MOC e portarias estaduais. O placeholder deste projeto deve ser substituído por implementação aderente antes do uso real.

---

## Estrutura de Comandos CLI (para recriar do zero)

```bash
# Criar projetos
dotnet new classlib -n Fiscal.Domain    -o src/Fiscal.Domain    --framework net10.0
dotnet new classlib -n Fiscal.Application -o src/Fiscal.Application --framework net10.0
dotnet new classlib -n Fiscal.Infrastructure -o src/Fiscal.Infrastructure --framework net10.0
dotnet new webapi   -n Fiscal.Api       -o src/Fiscal.Api       --framework net10.0

dotnet new xunit -n Fiscal.Domain.Tests          -o tests/Fiscal.Domain.Tests
dotnet new xunit -n Fiscal.Application.Tests     -o tests/Fiscal.Application.Tests
dotnet new xunit -n Fiscal.Infrastructure.Tests  -o tests/Fiscal.Infrastructure.Tests
dotnet new xunit -n Fiscal.Integration.Tests     -o tests/Fiscal.Integration.Tests

# Adicionar à solução
dotnet sln md-fiscal.slnx add src/**/*.csproj tests/**/*.csproj

# Referências entre projetos
dotnet add src/Fiscal.Application reference src/Fiscal.Domain
dotnet add src/Fiscal.Infrastructure reference src/Fiscal.Domain
dotnet add src/Fiscal.Infrastructure reference src/Fiscal.Application
dotnet add src/Fiscal.Api reference src/Fiscal.Application
dotnet add src/Fiscal.Api reference src/Fiscal.Infrastructure
dotnet add src/Fiscal.Api reference src/Fiscal.Domain

# Pacotes Infrastructure
dotnet add src/Fiscal.Infrastructure package System.Security.Cryptography.Xml
dotnet add src/Fiscal.Infrastructure package Microsoft.Extensions.Http
dotnet add src/Fiscal.Infrastructure package Microsoft.Extensions.Logging.Abstractions

# Pacotes de testes
dotnet add tests/Fiscal.Domain.Tests package FluentAssertions --version 7.0.0
dotnet add tests/Fiscal.Domain.Tests package NSubstitute
dotnet add tests/Fiscal.Domain.Tests package Microsoft.Extensions.Logging.Abstractions
# (repetir para os demais projetos de teste)
```

---

## Licença

Distribuído para fins educacionais e de referência arquitetural. Verifique os termos de uso dos schemas XSD e WSDLs junto ao [Portal Nacional da NF-e](https://www.nfe.fazenda.gov.br) e à SEFAZ-MG antes de uso em produção.

---

*Última atualização: Maio de 2026 — Leiaute NF-e 4.00 / MOC 7.0 / .NET 10.0.300*
