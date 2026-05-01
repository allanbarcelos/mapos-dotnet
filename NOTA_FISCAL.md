# Nota Fiscal Eletrônica (NFC-e) — Documentação de Implementação

> **⚠ Esta funcionalidade não está implementada nesta versão pública.**
>
> A emissão de notas fiscais homologadas **existe e está em produção** em versões dedicadas deste sistema, desenvolvidas sob demanda para clientes específicos. Cada implementação é realizada de forma personalizada, adaptada à realidade tributária, estado de operação e regime fiscal do cliente.
>
> Se você tem interesse em ter esta funcionalidade no seu sistema, **entre em contato** — a implementação é feita como serviço contratado, pois envolve responsabilidades legais, validação junto à SEFAZ, homologação em ambiente de testes, migração para produção e garantias sobre o correto funcionamento fiscal.
>
> Trata-se de um serviço com custo, não de uma contribuição gratuita, precisamente porque nota fiscal é um documento com implicações jurídicas e tributárias — e erros nessa área geram multas e autuações. A implementação inclui acompanhamento, testes e entrega em produção funcionando.
>
> **Contato:** allan@barcelos.dev

---

## Visão Geral

Para um PDV de varejo, o documento fiscal correto é a **NFC-e (Nota Fiscal de Consumidor Eletrônica, modelo 65)**. A tabela abaixo compara os documentos disponíveis para quem opera no varejo:

| Documento | Quando usar | Complexidade |
|-----------|-------------|--------------|
| **NFC-e** (mod. 65) | Venda a consumidor final — varejo e PDV | Média |
| **NF-e** (mod. 55) | Venda entre empresas — B2B | Alta |
| **SAT / CF-e** | Alternativa à NFC-e exclusiva para São Paulo | Média |

Esta documentação cobre exclusivamente a NFC-e, que é o modelo aplicável ao PDV deste sistema.

---

## Pré-requisitos Legais

Antes de qualquer implementação técnica, o estabelecimento precisa providenciar — fora do sistema — os seguintes itens junto aos órgãos competentes:

- **CNPJ ativo** com regime tributário definido (Simples Nacional, Lucro Presumido ou Lucro Real)
- **Inscrição Estadual** no estado de operação
- **Credenciamento na SEFAZ** do estado para emissão de NFC-e (processo online, feito pelo contador ou pelo próprio responsável legal)
- **Certificado Digital A1 ou A3** (e-CNPJ ou e-CPF) emitido por autoridade certificadora credenciada (Serpro, Certisign, Valid, etc.)
- **CSC (Código de Segurança do Contribuinte)** gerado no portal da SEFAZ após o credenciamento
- **ID do CSC** correspondente, fornecido no mesmo processo

Nenhum desses itens é responsabilidade do sistema — são obrigações legais do estabelecimento. A implementação técnica só pode começar após todos estarem regularizados.

---

## Dados Necessários no Sistema

### Emitente (`/Emitente`)

A tela de emitente precisaria ser ampliada com os seguintes campos obrigatórios para a NFC-e:

- CNPJ e Inscrição Estadual
- **CRT — Código de Regime Tributário:** 1 = Simples Nacional, 2 = Simples Nacional — Excesso de sublimite, 3 = Regime Normal
- Razão social e nome fantasia
- Endereço completo: logradouro, número, bairro, município, UF, CEP
- **Código IBGE do município** (obrigatório no XML da NFC-e — não é o CEP)

### Produtos

Cada produto precisa dos campos fiscais abaixo para compor os itens da nota:

- **NCM** — Nomenclatura Comum do Mercosul (8 dígitos obrigatórios)
- **CFOP** — Código Fiscal de Operações e Prestações (ex: 5102 para venda de mercadoria adquirida para comercialização)
- **CST ou CSOSN** — Tributação do ICMS: CST para Regime Normal, CSOSN para empresas do Simples Nacional
- Unidade comercial e tributável (UN, KG, CX, etc.)

### Configurações do Sistema

Novas chaves de configuração necessárias:

| Chave | Descrição |
|-------|-----------|
| `nfce_csc` | CSC criptografado |
| `nfce_id_csc` | ID do CSC correspondente |
| `nfce_serie` | Série da NFC-e (padrão: 1) |
| `nfce_proximo_numero` | Contador sequencial de emissão |
| `nfce_ambiente` | `1` = produção, `2` = homologação |
| `nfce_api_token` | Token da API fiscal (apenas na Opção A abaixo) |

---

## Abordagens de Implementação

Existem duas formas técnicas de integrar a emissão de NFC-e. A escolha depende do perfil do cliente, do volume de emissões e do orçamento disponível.

### Opção A — API de Terceiros (recomendada para a maioria dos casos)

Serviços como **Nuvem Fiscal**, **Focus NFe** ou **eNotas** recebem um JSON com os dados da venda e cuidam de toda a complexidade fiscal:

- Assinatura digital com o certificado (o certificado é armazenado por eles, não no servidor da aplicação)
- Transmissão para o WebService da SEFAZ no estado correto
- Gestão de contingência em caso de instabilidade da SEFAZ
- Cancelamento e inutilização de numeração
- Retornam a chave de acesso (44 dígitos), o protocolo de autorização e o XML assinado

**Vantagens:**
- Integração simples — um POST HTTP com os dados da venda
- Funciona para qualquer estado sem configuração extra
- A SEFAZ de cada estado tem WebServices diferentes; o provedor abstrai tudo isso
- Plano gratuito disponível em homologação

**Desvantagem:**
- Custo mensal recorrente por volume de documentos emitidos

### Opção B — Biblioteca .NET Direta

Bibliotecas open source que montam, assinam e transmitem o XML diretamente para a SEFAZ, sem intermediário:

| Biblioteca | Repositório |
|------------|-------------|
| DFe.NET | github.com/dfe-net/DFe.NET |
| NFe.Core | github.com/gustavoferreira/NFeCore |

**Vantagens:**
- Sem custo por emissão
- Controle total do processo

**Desvantagens:**
- Complexidade de implementação consideravelmente maior
- O certificado `.pfx` precisa ser armazenado e protegido no servidor da aplicação
- Os WebServices da SEFAZ variam por estado — manutenção contínua necessária

Para instalações em produção, a Opção A tem sido a escolha adotada por oferecer mais estabilidade, menor risco operacional e responsabilidade compartilhada com um provedor especializado.

---

## Fluxo Técnico no PDV

```
Operador finaliza venda no PDV
        ↓
Sistema monta o objeto NFC-e
(emitente + destinatário + itens com NCM/CFOP/CST + totais + formas de pagamento)
        ↓
[Opção A] POST para API do provedor   /   [Opção B] Assinatura local com .pfx
        ↓
Transmissão para WebService SEFAZ (produção ou homologação)
        ↓
Resposta: chave de acesso (44 dígitos) + protocolo de autorização
        ↓
XML autorizado salvo no banco de dados
        ↓
Cupom térmico impresso com QR Code, chave de acesso e URL de consulta
```

Em caso de falha de comunicação com a SEFAZ, entra o modo de **contingência offline (EPEC)** — o documento é emitido localmente e transmitido à SEFAZ assim que a conexão for restabelecida.

---

## Mudanças Necessárias no Código

### Banco de dados — novos campos e tabela

**`produtos`** — campos fiscais adicionais:
```sql
ncm         VARCHAR(8)   -- Nomenclatura Comum do Mercosul (obrigatório)
cfop        VARCHAR(4)   -- ex: 5102
cst_icms    VARCHAR(3)   -- CST (regime normal) ou CSOSN (Simples Nacional)
```

**`emitente`** — campos fiscais adicionais:
```sql
crt                    INTEGER      -- 1, 2 ou 3
codigo_ibge_municipio  VARCHAR(7)   -- código IBGE do município sede
ie                     VARCHAR(20)  -- Inscrição Estadual
```

**`nfce`** — nova tabela para armazenar as notas emitidas:
```sql
id              SERIAL PRIMARY KEY
venda_id        INTEGER REFERENCES vendas(id)
chave_acesso    VARCHAR(44)
numero          INTEGER
serie           INTEGER
protocolo       VARCHAR(20)
xml_autorizado  TEXT
danfe_qrcode    TEXT
status          VARCHAR(20)  -- autorizada | cancelada | contingencia
emitido_em      TIMESTAMP
```

### Contrato do serviço fiscal

```csharp
public interface IFiscalService
{
    Task<ResultadoEmissao> EmitirNfceAsync(int vendaId);
    Task<ResultadoEmissao> CancelarNfceAsync(string chaveAcesso, string justificativa);
}
```

### Integração no fluxo de venda (`Pdv/Index.cshtml.cs`)

Após a confirmação da transação (`tx.CommitAsync()`), antes de retornar o resultado ao frontend:

```csharp
var resultadoFiscal = await fiscalService.EmitirNfceAsync(venda.Id);
// retornar chave de acesso e QR Code para o frontend incluir na impressão do cupom
```

### Cupom térmico — campos adicionais

O cupom já impresso via ESC/POS precisaria incluir:
- **QR Code** da NFC-e (gerado a partir da chave de acesso + CSC)
- Chave de acesso formatada (44 dígitos, em blocos de 4)
- URL de consulta pública da SEFAZ
- Número, série e data/hora de emissão

---

## Ordem de Implementação

1. O estabelecimento providencia os pré-requisitos legais (CNPJ, IE, credenciamento SEFAZ, certificado digital, CSC)
2. Preenchimento dos campos fiscais nos produtos (NCM, CFOP, CST/CSOSN) e no emitente (CRT, código IBGE, IE)
3. Escolha da abordagem — API de terceiros ou biblioteca direta
4. Configuração do ambiente de **homologação** — a SEFAZ disponibiliza ambiente de testes gratuito
5. Implementação e testes com 10–20 emissões em homologação
6. Validação com contador do cliente
7. Migração para **produção**

---

## Referências

- NFC-e — Especificação técnica: portais das Secretarias de Fazenda estaduais
- Nuvem Fiscal: nuvemfiscal.com.br
- Focus NFe: focusnfe.com.br
- eNotas: enotas.com.br
- DFe.NET: github.com/dfe-net/DFe.NET
- Consulta NCM: tabelancm.com
- Código IBGE por município: ibge.gov.br/cidades-e-estados
