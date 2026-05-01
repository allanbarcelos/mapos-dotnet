# MercadoPago — Ativação e Testes com Sandbox

## Sumário

1. [Criar conta e obter credenciais](#1-criar-conta-e-obter-credenciais)
2. [Configurar no projeto](#2-configurar-no-projeto)
3. [Preparar o cliente para teste](#3-preparar-o-cliente-para-teste)
4. [Gerar um boleto (sandbox)](#4-gerar-um-boleto-sandbox)
5. [Simular pagamento no painel MP](#5-simular-pagamento-no-painel-mp)
6. [Sincronizar status na aplicação](#6-sincronizar-status-na-aplicação)
7. [Referência: status e mapeamento](#7-referência-status-e-mapeamento)
8. [Ir para produção](#8-ir-para-produção)
9. [Solução de problemas](#9-solução-de-problemas)

---

## 1. Criar conta e obter credenciais

### 1.1 Acesse o painel de desenvolvedores

1. Acesse **https://www.mercadopago.com.br/developers/panel/credentials**
2. Faça login com sua conta MercadoPago (ou crie uma em `mercadopago.com.br`)

### 1.2 Credenciais de teste (Sandbox)

1. No painel, clique em **"Credenciais de teste"**
2. Copie o **Access Token de teste** — começa com `TEST-`

   ```
   TEST-1234567890abcdef-...
   ```

3. Guarde também o **Public Key de teste** (não é usado pela aplicação, mas útil para depuração)

> **Importante:** o Access Token de teste funciona apenas com cartões, CPFs e e-mails de teste
> fornecidos pelo próprio MercadoPago. Veja a [seção 3](#3-preparar-o-cliente-para-teste).

---

## 2. Configurar no projeto

### 2.1 Modo Docker (produção / staging)

Edite o arquivo `.env` na raiz do projeto:

```env
# MercadoPago — Access Token de TESTE (começa com TEST-)
MP_ACCESS_TOKEN=TEST-seu-access-token-aqui

# Validade do boleto em formato ISO 8601
# P3D = 3 dias | P7D = 7 dias | PT48H = 48 horas
MP_BOLETO_EXPIRATION=P3D
```

Depois gere os secrets e suba a stack:

```bash
make setup   # lê o .env e grava em secrets/
make up      # docker compose up --build
```

### 2.2 Modo local (`dotnet run`)

Exporte as variáveis antes de rodar:

```bash
export MercadoPago__AccessToken="TEST-seu-access-token-aqui"
export MercadoPago__BoletoExpiration="P3D"
dotnet run
```

Ou use `appsettings.Development.json` (**nunca faça commit deste arquivo com credenciais reais**):

```json
{
  "MercadoPago": {
    "AccessToken": "TEST-seu-access-token-aqui",
    "BoletoExpiration": "P3D"
  }
}
```

### 2.3 Verificar se está ativo

Com o Access Token configurado, o botão **"Gerar Boleto"** aparece automaticamente nas páginas de
visualização de OS e Venda. Se o botão não aparecer, o token não foi lido pela aplicação.

---

## 3. Preparar o cliente para teste

O sandbox do MercadoPago exige dados específicos — não aceita CPF/CNPJ, e-mails ou endereços
aleatórios.

### 3.1 Cadastrar (ou editar) um cliente de teste

No Map-OS, acesse **Clientes → Editar** e preencha com os dados abaixo:

| Campo          | Valor de exemplo                    |
|----------------|-------------------------------------|
| **Nome**       | Teste Sandbox                       |
| **CPF/CNPJ**   | `12345678909` (CPF de teste MP)     |
| **E-mail**     | `test_user_123456@testuser.com`     |
| **Telefone**   | `11999999999`                       |
| **Rua**        | Rua de Teste                        |
| **Número**     | `123`                               |
| **Bairro**     | Centro                              |
| **Cidade**     | São Paulo                           |
| **Estado**     | SP                                  |
| **CEP**        | `01310100`                          |

> **CPFs válidos para teste no MP:** `12345678909`, `19119119100`, `23232323232`
>
> **E-mail:** pode usar qualquer e-mail no formato `test_user_XXXXXX@testuser.com`, ou usar
> um e-mail de conta de teste criada em:
> https://www.mercadopago.com.br/developers/panel/test-users

### 3.2 (Opcional) Criar usuário de teste oficial

Para ter um ambiente totalmente isolado:

1. Acesse **https://www.mercadopago.com.br/developers/panel/test-users**
2. Crie um usuário de teste (tipo "vendedor" ou "comprador")
3. Use as credenciais do **vendedor** como `MP_ACCESS_TOKEN`

---

## 4. Gerar um boleto (sandbox)

### 4.1 Via OS

1. Acesse **Ordens de Serviço → Visualizar** de uma OS com valor > 0
2. Na seção **"Cobrança MercadoPago"**, clique em **"Gerar Boleto"**
3. Se todos os dados do cliente estiverem corretos, a cobrança é criada

### 4.2 Via Venda

1. Acesse **Vendas → Visualizar** de uma venda com valor > 0
2. Clique em **"Gerar Boleto"** na seção de cobrança

### 4.3 O que acontece

- A aplicação chama `POST /v1/payments` na API do MercadoPago
- É criado um registro na tabela `cobrancas` com:
  - `payment_gateway = 'mercadopago'`
  - `charge_id` = ID do pagamento retornado pela API
  - `status = 'PENDING'`
  - `barcode` = linha digitável do boleto
  - `pdf` = link para o PDF do boleto

### 4.4 Visualizar a cobrança criada

Acesse **Cobranças → Visualizar** da cobrança gerada. Você verá:

- **Código de barras** do boleto
- **Link para o PDF** do boleto (abrirá em nova aba)
- Botões para **Atualizar**, **Cancelar** e **Confirmar Pagamento**

---

## 5. Simular pagamento no painel MP

No sandbox, o boleto não é pago via banco — você precisa aprovar manualmente.

### 5.1 Via painel do desenvolvedor

1. Acesse **https://www.mercadopago.com.br/developers/panel/activity**
2. Localize o pagamento pelo ID (visível em **Cobranças → Visualizar** → campo Charge ID)
3. Clique no pagamento → **"Simular pagamento"** → selecione **"Aprovado"**

### 5.2 Via API diretamente (curl)

```bash
# Substitua {PAYMENT_ID} pelo ID da cobrança e {ACCESS_TOKEN} pelo token de teste
curl -X PUT \
  https://api.mercadopago.com/v1/payments/{PAYMENT_ID} \
  -H "Authorization: Bearer {ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"status": "approved"}'
```

Outros status que podem ser simulados:

| Status API         | Significado               |
|--------------------|---------------------------|
| `approved`         | Pago / aprovado           |
| `pending`          | Pendente (padrão boleto)  |
| `cancelled`        | Cancelado                 |
| `in_process`       | Em processamento          |
| `rejected`         | Rejeitado                 |

---

## 6. Sincronizar status na aplicação

Após simular o pagamento no painel MP, atualize o status na aplicação:

1. Acesse **Cobranças → Visualizar** da cobrança
2. Na seção **"Gateway MercadoPago"**, clique em **"Atualizar do Gateway"**
3. O status será atualizado para `RECEIVED` (se aprovado) ou o status correspondente

### Ações disponíveis

| Botão                    | O que faz                                                    |
|--------------------------|--------------------------------------------------------------|
| **Atualizar do Gateway** | Busca o status atual da API e sincroniza localmente          |
| **Cancelar no Gateway**  | Cancela o pagamento na API e atualiza o status para CANCELLED |
| **Confirmar Pagamento**  | Para pagamentos `authorized` — captura o valor              |

---

## 7. Referência: status e mapeamento

A aplicação mapeia os status da API do MercadoPago para status internos:

| Status API MP       | Status interno | Significado                     |
|---------------------|----------------|---------------------------------|
| `approved`          | `RECEIVED`     | Pagamento confirmado/recebido   |
| `authorized`        | `AUTHORIZED`   | Autorizado, aguardando captura  |
| `in_process`        | `PROCESSING`   | Em análise / em processamento   |
| `pending`           | `PENDING`      | Boleto gerado, aguardando pag.  |
| `cancelled`         | `CANCELLED`    | Cancelado                       |
| `refunded`          | `REFUNDED`     | Estornado                       |
| `charged_back`      | `CHARGED_BACK` | Chargeback                      |
| outros              | `PENDING`      | Fallback                        |

### Validade do boleto (`MP_BOLETO_EXPIRATION`)

Formato ISO 8601 Duration:

| Valor    | Significado  |
|----------|--------------|
| `P1D`    | 1 dia        |
| `P3D`    | 3 dias       |
| `P7D`    | 7 dias       |
| `P30D`   | 30 dias      |
| `PT48H`  | 48 horas     |
| `PT72H`  | 72 horas     |

---

## 8. Ir para produção

Quando os testes estiverem OK, troque pelo **Access Token de produção**:

1. No painel MP, acesse **"Credenciais de produção"**
2. Copie o Access Token — começa com `APP_USR-`

   ```
   APP_USR-1234567890abcdef-...
   ```

3. Atualize o `.env`:

   ```env
   MP_ACCESS_TOKEN=APP_USR-seu-token-de-producao
   ```

4. Execute `make setup && make up` para aplicar

> **Atenção:** com o token de produção, os boletos são reais e cobram o cliente.
> Nunca commite o Access Token de produção no repositório.

---

## 9. Solução de problemas

### Botão "Gerar Boleto" não aparece

- Verifique se `MP_ACCESS_TOKEN` está definido: `make setup` deve mostrar o arquivo `secrets/mp_access_token`
- Reinicie o container: `docker compose restart app`
- Em modo local, verifique se a variável de ambiente está exportada no mesmo shell

### Erro "Cliente sem endereço / CPF / e-mail"

- Todos os campos obrigatórios do cliente precisam estar preenchidos
- Veja a tabela na [seção 3.1](#31-cadastrar-ou-editar-um-cliente-de-teste)

### Erro HTTP 400 / 401 da API

| Código | Causa provável                                        |
|--------|-------------------------------------------------------|
| 401    | Access Token inválido ou expirado                     |
| 400    | Dados inválidos (CPF de teste não aceito, valor zero) |
| 400    | `payment_method_id` inválido para o país da conta     |

> O método de pagamento padrão é `bolbradesco`. Se sua conta for de outro país, pode ser
> necessário ajustar `PaymentMethodId` no `MercadoPagoService.cs`.

### Como ver o payload enviado à API

Ative os logs em `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Cobrança criada mas status não atualiza

O MercadoPago não envia webhooks automaticamente no sandbox para boleto. Use o botão
**"Atualizar do Gateway"** para buscar o status manualmente, ou configure um webhook em:
**https://www.mercadopago.com.br/developers/panel/webhooks**
