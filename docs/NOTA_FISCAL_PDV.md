# Emissão de Notas Fiscais Oficiais no PDV

## Tipos de documento

Para um PDV de varejo, o documento correto é a **NFC-e (Nota Fiscal de Consumidor Eletrônica, modelo 65)**.

| Documento | Quem usa | Complexidade |
|-----------|----------|-------------|
| **NFC-e** (mod. 65) | Varejo/PDV — venda a consumidor final | Média |
| **NF-e** (mod. 55) | Venda entre empresas (B2B) | Alta |
| **SAT / CF-e** | Alternativa à NFC-e, exclusivo para São Paulo | Média |

---

## Pré-requisitos legais (fora do sistema)

Antes de qualquer implementação técnica, o estabelecimento precisa:

- **CNPJ ativo** com regime tributário definido (Simples Nacional, Lucro Presumido, etc.)
- **Inscrição Estadual** no estado de operação
- **Credenciamento na SEFAZ** do estado para emissão de NFC-e
- **Certificado Digital A1 ou A3** (e-CNPJ ou e-CPF) emitido por autoridade certificadora (Serpro, Certisign, Valid, etc.)
- **CSC (Código de Segurança do Contribuinte)** gerado no portal da SEFAZ do estado
- **ID do CSC** correspondente ao CSC cadastrado

---

## Dados que precisam estar no sistema

### Emitente (`/Emitente`)
- CNPJ e Inscrição Estadual
- CRT — Código de Regime Tributário (1 = Simples Nacional, 3 = Regime Normal)
- Razão social e nome fantasia
- Endereço completo: logradouro, número, bairro, município, UF, CEP
- **Código IBGE do município** (obrigatório no XML da NFC-e)

### Produtos
- **NCM** — Nomenclatura Comum do Mercosul (8 dígitos)
- **CFOP** — Código Fiscal de Operações e Prestações
- **CST ou CSOSN** — tributação do ICMS (CST para regime normal, CSOSN para Simples)
- Unidade comercial e tributável (UN, KG, CX, etc.)

### Configurações do sistema
- CSC e ID do CSC (por UF)
- Token / credenciais da API fiscal (se usar serviço de terceiros)
- Ambiente: **homologação** (testes) ou **produção**

---

## Abordagens de implementação

### Opção A — API de terceiros (recomendada)

Serviços como **Nuvem Fiscal**, **Focus NFe** ou **eNotas** recebem um JSON com os dados da venda e cuidam de toda a complexidade:

- Assinatura digital com o certificado
- Transmissão para o WebService da SEFAZ
- Gestão de contingência (modo offline)
- Cancelamento e inutilização de numeração
- Retornam a chave de acesso e o XML autorizado

**Vantagens:** não é necessário gerenciar o certificado digital no servidor; a integração é um simples POST HTTP; funcionam para qualquer estado sem configuração extra.

**Desvantagem:** custo mensal por documento emitido (a maioria tem plano gratuito para homologação).

### Opção B — Biblioteca .NET direta

Bibliotecas open source que montam, assinam e transmitem o XML diretamente para a SEFAZ:

| Biblioteca | Repositório | Observação |
|------------|-------------|------------|
| DFe.NET | github.com/dfe-net/DFe.NET | Completa, suporta NF-e, NFC-e, CT-e, MDF-e |
| NFe.Core | github.com/gustavoferreira/NFeCore | Focada em NF-e / NFC-e |

**Vantagens:** sem custo por emissão; controle total do processo.

**Desvantagem:** maior complexidade de implementação; o certificado `.pfx` precisa ser armazenado e protegido no servidor; WebServices SEFAZ variam por estado.

---

## Fluxo técnico no PDV

```
Finalizar venda no PDV
        ↓
Montar objeto NFC-e
(emitente + destinatário + itens + totais + formas de pagamento)
        ↓
Assinar XML com certificado digital (A1/A3)
        ↓
Transmitir para WebService SEFAZ
(produção ou homologação, endpoint varia por estado)
        ↓
Receber autorização: chave de acesso + protocolo
        ↓
Gerar DANFE NFC-e com QR Code para impressão no cupom
        ↓
Armazenar XML autorizado + chave no banco de dados
```

Em caso de falha de comunicação com a SEFAZ, entra o modo de **contingência offline (EPEC)** — o documento é emitido localmente e transmitido posteriormente.

---

## Mudanças necessárias no código

### 1. Banco de dados — novos campos

**Tabela `produtos`**
```sql
ncm         VARCHAR(8)   -- NCM obrigatório
cfop        VARCHAR(4)   -- ex: 5102
cst_icms    VARCHAR(3)   -- CST (regime normal) ou CSOSN (Simples)
```

**Tabela `emitente`**
```sql
crt             INTEGER      -- 1, 2 ou 3
codigo_ibge_municipio  VARCHAR(7)
ie              VARCHAR(20)  -- Inscrição Estadual
```

**Tabela `nfce`** (nova)
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

**Tabela `configuracoes`** — novas chaves
```
nfce_csc        -- CSC criptografado
nfce_id_csc     -- ID do CSC
nfce_serie      -- série da NFC-e (padrão: 1)
nfce_proximo_numero
nfce_ambiente   -- 1=produção, 2=homologação
nfce_api_token  -- token da API fiscal (se Opção A)
```

### 2. Novo serviço

```csharp
public interface IFiscalService
{
    Task<ResultadoEmissao> EmitirNfceAsync(int vendaId);
    Task<ResultadoEmissao> CancelarNfceAsync(string chaveAcesso, string justificativa);
}
```

### 3. Integração no `Pdv/Index.cshtml.cs`

No método `OnPostFinalizarVendaAsync`, após `tx.CommitAsync()`:

```csharp
var resultadoFiscal = await fiscalService.EmitirNfceAsync(venda.Id);
// retornar chave de acesso para o frontend imprimir o QR Code no cupom
```

### 4. Impressão do cupom térmico

O cupom já impresso via ESC/POS precisa incluir:
- **QR Code** da NFC-e (gerado a partir da chave de acesso + CSC)
- Chave de acesso formatada (44 dígitos)
- URL de consulta da SEFAZ
- Número, série e data de emissão

---

## Ordem de implementação sugerida

1. Credenciar o CNPJ na SEFAZ do estado (responsabilidade do cliente)
2. Completar dados do emitente e produtos (NCM, CFOP, CST)
3. Escolher Opção A (API terceiros) ou B (biblioteca direta)
4. Implementar em **homologação** — a SEFAZ disponibiliza ambiente de testes gratuito
5. Validar 10–20 emissões de teste
6. Migrar para **produção**

---

## Referências

- Especificação técnica NFC-e: portal.fazenda.sp.gov.br (e equivalentes estaduais)
- Nuvem Fiscal: nuvemfiscal.com.br
- Focus NFe: focusnfe.com.br
- DFe.NET: github.com/dfe-net/DFe.NET
- Consulta NCM: tabelancm.com
