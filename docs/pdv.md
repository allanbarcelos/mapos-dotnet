# PDV — Frente de Caixa

O módulo PDV (Ponto de Venda) oferece uma interface de frente de caixa completa com controle de sessão, múltiplas formas de pagamento, busca de produtos e impressão de cupom não fiscal em impressoras térmicas via socket TCP.

---

## Permissões

| Código  | Descrição                                          |
|---------|----------------------------------------------------|
| `vPdv`  | Acessa o PDV e visualiza histórico de sessões      |
| `fPdv`  | Pode fechar o caixa (recomendado só para gerentes) |

Configure em **Configurações → Permissões** para o perfil desejado.

---

## Fluxo de trabalho

```
Abrir Caixa  →  Frente de Caixa  →  (vendas)  →  Fechar Caixa  →  Histórico
```

> Ações especiais (desconto, remoção de item, alteração de preço) exigem **autorização do Fiscal PDV** antes de serem executadas. Veja a seção [Fiscal PDV](#fiscal-pdv).

### 1. Abrir Caixa

Acesse **PDV** no menu lateral. Se não houver sessão aberta, o sistema redireciona para a tela de **Abertura de Caixa**:

- Informe o **saldo inicial** (dinheiro em espécie já disponível no caixa).
- Clique em **Abrir Caixa**.

Uma sessão (`SessaoCaixa`) é criada vinculada ao operador logado.

### 2. Frente de Caixa

A tela principal divide-se em duas colunas:

**Coluna esquerda — Busca de produtos**
- Digite o nome ou código de barras no campo de busca.
- Os resultados aparecem em tempo real (mínimo 2 caracteres).
- Se o termo digitado coincidir **exatamente** com um código de barras, o produto é adicionado automaticamente ao carrinho.
- Clique em qualquer linha da lista para adicionar 1 unidade ao carrinho.

**Coluna direita — Carrinho e pagamento**
- Ajuste quantidade com os botões `+` / `-` ou clicando no número.
- Remova um item clicando no ícone `×` — requer **autorização fiscal**.
- Altere o preço unitário clicando no ícone de lápis ao lado do preço — requer **autorização fiscal**.
- Aplique desconto em **reais** ou **percentual** no campo desconto — requer **autorização fiscal**.
- Selecione a forma de pagamento:
  - **Dinheiro** — habilita campos "Valor Recebido" e "Troco".
  - **PIX** — sem troco.
  - **Débito** / **Crédito** / **Fiado** — sem troco.
- Clique em **Finalizar Venda** (ou **Finalizar Ação** quando há autorização fiscal ativa).
- Após confirmação, o sistema oferece a opção de **Imprimir Cupom** (requer impressora configurada).

### 3. Fechar Caixa

Clique em **Fechar Caixa** no cabeçalho do PDV (ou acesse `/Pdv/FecharCaixa/{id}`).

- O sistema exibe o resumo da sessão (total por forma de pagamento).
- Informe o **valor contado fisicamente** no caixa.
- O sistema calcula a diferença (sobra ou falta).
- Confirme o fechamento. A sessão passa para status `fechada` e não pode ser reaberta.

### 4. Histórico

Acesse **PDV → Histórico** (ou `/Pdv/Historico`) para ver todas as sessões anteriores com seus totais e diferenças de caixa.

---

## Atalhos de teclado

| Tecla   | Ação                                          |
|---------|-----------------------------------------------|
| `F1`    | Selecionar forma de pagamento **Dinheiro**    |
| `F2`    | Selecionar forma de pagamento **PIX**         |
| `F3`    | Selecionar forma de pagamento **Débito**      |
| `F4`    | Selecionar forma de pagamento **Crédito**     |
| `F5`    | Selecionar forma de pagamento **Fiado**       |
| `Space` | Finalizar Venda / Finalizar Ação              |
| `Enter` | Adicionar produto ao pesquisar por código de barras |

> `Space` só é acionado quando o foco **não** está em nenhum campo de texto.

---

## Fiscal PDV

O Fiscal PDV é um mecanismo de controle que exige a presença e autenticação de um operador autorizado para executar ações que alteram o valor da venda: **desconto**, **remoção de item** e **alteração de preço unitário**.

### Habilitando um usuário como Fiscal PDV

1. Acesse **Usuários → Editar** (ou **Usuários → Novo**) para o usuário desejado.
2. Na seção **Fiscal PDV** (visível apenas para administradores com permissão `cSistema`):
   - Marque o checkbox **Habilitar Fiscal PDV**.
   - Informe um **PIN** de 4 a 10 dígitos (o PIN é armazenado com hash BCrypt).
3. Salve o usuário.

O sistema gera automaticamente um **código único** para o cartão fiscal no formato `FISCALxxxxxxxxxxxxxxxx` (20 caracteres alfanuméricos maiúsculos).

> Para revogar o acesso fiscal, desmarque o checkbox e salve. O código e o PIN são apagados permanentemente.

### Cartão Fiscal PDV

Após habilitar o fiscal em um usuário, o botão **Baixar / Imprimir Cartão Fiscal** é exibido na tela de edição do usuário.

O cartão segue o padrão **ISO/IEC 7810 ID-1** (85,6 × 54 mm — tamanho de cartão de crédito) e contém:

- Nome do operador
- Código de barras **CODE128** com o código único
- Identificador numérico do usuário

Imprima o cartão em cartolina ou plastifique para uso diário no caixa.

### Fluxo de autorização fiscal no PDV

Quando o caixa tenta executar uma ação especial (desconto, remoção, alteração de preço), o sistema exibe automaticamente o **modal de Autorização Fiscal**:

```
┌─────────────────────────────────┐
│  🛡  Autorização Fiscal          │
│  "Aplicação de desconto requer  │
│   autorização do Fiscal PDV."   │
│                                 │
│  Código do cartão               │
│  [ _____________________ ]  ←── escaneie o código de barras
│                                 │
│  PIN fiscal                     │
│  [ •••• ]                       │
│                                 │
│  [ Confirmar Autorização ]      │
│  [ Cancelar ]                   │
└─────────────────────────────────┘
```

**Passos:**

1. O fiscal posiciona o cartão na leitora de código de barras → o campo "Código do cartão" é preenchido automaticamente e o foco avança para o PIN.
2. O fiscal digita o PIN e pressiona `Enter` ou clica em **Confirmar Autorização**.
3. O servidor valida o código (busca o usuário por `FiscalPdvCodigo`) e verifica o PIN com BCrypt.
4. Se aprovado:
   - A ação é executada imediatamente (remoção / alteração de preço).
   - Para desconto: o valor digitado é mantido.
   - O botão **Finalizar Venda** muda para **Finalizar Ação** (cor âmbar) e exibe o nome do fiscal autorizado acima do botão.
5. Se recusado: mensagem de erro é exibida no modal (código inválido ou PIN incorreto).

> Cancelar o modal de desconto **zera** o campo de desconto automaticamente.

### Estado de autorização ativa

Enquanto houver autorização fiscal na sessão de venda atual:

- O ícone de cadeado ao lado de "Desconto" aparece **desbloqueado** (verde).
- O nome do fiscal aparece no rodapé da área de pagamento.
- O botão de finalização mostra **FINALIZAR AÇÃO** com fundo âmbar.

A autorização é **resetada** automaticamente quando:
- A venda é finalizada com sucesso.
- O carrinho é limpo manualmente.

### Segurança

- O PIN nunca é armazenado em texto claro — apenas o hash BCrypt.
- A verificação ocorre exclusivamente no servidor (`OnPostAutorizarFiscalAsync`).
- Cada autorização gera um registro de auditoria: `PDV: Autorização fiscal por <nome> (ação: <tipo>)`.
- Apenas usuários com `FiscalPdv = true` e `Situacao = true` (ativos) podem autorizar.
- Somente administradores (`cSistema`) podem habilitar/revogar o flag fiscal.

---

## Integração com MercadoPago Point (maquininha)

Ao selecionar **PIX**, **Débito** ou **Crédito** como forma de pagamento e finalizar a venda, o sistema envia automaticamente uma **intenção de pagamento** para a maquininha configurada, usando a API REST do MercadoPago Point.

### Configuração

Acesse **Configurações → PDV — MercadoPago Point**:

| Campo              | Descrição                                              |
|--------------------|--------------------------------------------------------|
| Access Token MP    | Token de acesso da conta MercadoPago (começa com `APP_USR-...`) |
| Device ID          | Identificador do dispositivo Point (ex: `PAX_A910__SMARTPOS...`) |

Deixe ambos os campos em branco para desabilitar a integração (as vendas são registradas normalmente sem enviar para a maquininha).

### Fluxo

1. Venda é salva no banco de dados com status `Faturado`.
2. O sistema cria uma `payment_intent` via `POST /point/integration-api/devices/{deviceId}/payment-intents`.
3. O modal **"Aguardando pagamento"** é exibido com spinner e o valor da venda.
4. O frontend faz polling a cada **2 segundos** em `GET /Pdv/Index?handler=StatusPagamento&id={intentId}`.
5. Ao receber status `FINISHED` com `estado = approved` → modal muda para **"Pagamento Confirmado"** com opção de imprimir recibo.
6. Se `CANCELED` ou `ERROR` → modal exibe mensagem de erro.
7. O operador pode cancelar a intenção a qualquer momento clicando em **Cancelar** no modal.

### Estados da intenção de pagamento

| Status MP     | Comportamento no PDV                    |
|---------------|-----------------------------------------|
| `OPEN`        | Aguardando (polling continua)           |
| `ON_TERMINAL` | Aguardando (polling continua)           |
| `PROCESSING`  | Aguardando (polling continua)           |
| `FINISHED`    | Verifica `estado`: `approved` = OK, demais = erro |
| `CANCELED`    | Exibe erro "Pagamento cancelado"        |
| `ERROR`       | Exibe erro "Erro na maquininha"         |

---

## Configurações da Impressora Térmica

Acesse **Configurações → PDV — Impressora Térmica**.

| Campo                      | Descrição                                                     | Padrão        |
|----------------------------|---------------------------------------------------------------|---------------|
| Host da Impressora         | IP ou hostname da impressora na rede local                   | (vazio)       |
| Porta TCP                  | Porta RAW da impressora                                       | `9100`        |
| Largura do Papel           | 58 mm (32 colunas) ou 80 mm (48 colunas)                     | 48 colunas    |
| Gerar Lançamento Financeiro| Cria receita no módulo financeiro ao finalizar a venda       | Ativado       |
| Cabeçalho do Cupom         | Texto centralizado em negrito no topo do cupom               | (vazio)       |
| Rodapé do Cupom            | Texto centralizado no final do cupom                         | (vazio)       |

> **Dica**: deixe o campo **Host** em branco para desabilitar a impressão. A venda será registrada normalmente sem tentar imprimir.

### Exemplo de cabeçalho

```
Loja Exemplo Ltda
Rua das Flores, 100 — Centro
Tel: (11) 99999-0000
CNPJ: 00.000.000/0001-00
```

Cada linha do textarea corresponde a uma linha no cupom. O texto é centralizado e impresso em negrito.

---

## Protocolo de impressão

A impressão usa o protocolo **ESC/POS** via conexão TCP (RAW printing), compatível com impressoras da família:

- Epson TM-T20, TM-T88
- Bematech MP-4200 TH, MP-2800 TH
- Elgin i7, i9
- Daruma DR800, DR700
- Qualquer impressora com modo RAW na porta 9100

A conexão é estabelecida **pelo servidor** (ASP.NET) para a impressora configurada. O timeout de conexão é de 5 segundos.

### Estrutura do cupom

```
================================
      NOME DA LOJA
   Endereço, Nº — Cidade/UF
   Tel: (00) 00000-0000
================================
       CUPOM NÃO FISCAL
================================
PRODUTO A             2x  R$10,00
PRODUTO B             1x  R$ 5,00
--------------------------------
SUBTOTAL               R$25,00
DESCONTO               R$ 0,00
================================
     TOTAL     R$ 25,00
================================
PAGAMENTO: Dinheiro
RECEBIDO:             R$30,00
TROCO:                 R$ 5,00
--------------------------------
Venda #42 | 30/04/2026 14:32
--------------------------------
      Obrigado pela preferência!
            Volte sempre!
================================
```

---

## Lançamentos Financeiros

Quando a opção **Gerar Lançamento Financeiro** está ativada, cada venda finalizada no PDV cria automaticamente uma receita baixada no módulo financeiro com:

- **Descrição**: `PDV Venda #<id>`
- **Valor**: total da venda (após desconto)
- **Data**: data atual
- **Status**: baixado
- **Forma de pagamento**: conforme selecionado

---

## Controle de Estoque

Se **Controle de Estoque** estiver ativado nas configurações gerais, o estoque de cada produto é debitado automaticamente ao finalizar uma venda PDV.

---

## Cliente Padrão

Ao finalizar uma venda sem selecionar cliente, o sistema usa (ou cria automaticamente) um cliente chamado **"Consumidor Final"**. Para associar um cliente específico, use o campo de busca de clientes no PDV (funcionalidade a ser implementada em versão futura).

---

## Diagrama de banco de dados

```
usuarios (fiscal_pdv, fiscal_pdv_codigo, fiscal_pdv_pin)
  └── sessoes_caixa (OperadorId → usuarios.id)
        └── vendas (SessaoCaixaId → sessoes_caixa.id)
              └── itens_venda (VendaId → vendas.id)
```

### Tabela `sessoes_caixa`

| Coluna           | Tipo           | Descrição                             |
|------------------|----------------|---------------------------------------|
| id               | int PK         |                                       |
| aberto_em        | datetime       | UTC                                   |
| fechado_em       | datetime?      | UTC, preenchido ao fechar             |
| saldo_inicial    | decimal(10,2)  | Dinheiro informado na abertura        |
| saldo_esperado   | decimal(10,2)  | Inicial + entradas em dinheiro        |
| saldo_informado  | decimal(10,2)? | Contado fisicamente no fechamento     |
| diferenca        | decimal(10,2)? | SaldoInformado − SaldoEsperado        |
| status           | varchar(10)    | `aberta` \| `fechada`                 |
| observacoes      | text?          |                                       |
| operador_id      | int FK         | → usuarios.id                         |

### Colunas Fiscal PDV em `usuarios`

| Coluna              | Tipo         | Descrição                                           |
|---------------------|--------------|-----------------------------------------------------|
| fiscal_pdv          | bool         | Se o usuário é operador fiscal PDV (padrão: false)  |
| fiscal_pdv_codigo   | varchar(40)? | Código único do cartão (gerado automaticamente)     |
| fiscal_pdv_pin      | varchar(200)?| Hash BCrypt do PIN de 4–10 dígitos                  |

Migração EF: `AddFiscalPdv`.
