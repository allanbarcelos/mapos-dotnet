# Map-OS .NET

Sistema de gestão empresarial para pequenas e médias empresas — ordens de serviço, vendas, financeiro e PDV profissional.

> **Inspirado no [Map-OS](https://github.com/RamonSilva20/mapos)**, projeto open source original criado por **[Ramon Silva](https://github.com/RamonSilva20)** e mantido pela comunidade, escrito em PHP/CodeIgniter 3.
>
> Esta é uma **reimplementação totalmente remodelada em ASP.NET Core 9 (.NET)** — não uma portagem linha a linha. A arquitetura, a modelagem de dados, o sistema de permissões, o PDV e diversas funcionalidades foram redesenhados do zero com features próprias que não existem na versão original.

Este sistema existe há **mais de 3 anos e está em produção ativa**. A decisão de disponibilizá-lo publicamente vem da vontade de contribuir com a comunidade open source — especialmente diante do surgimento crescente de projetos gerados por IA sem critério de segurança, sem testes em produção real e sem organização de código. Este projeto foi construído e validado ao longo de anos de uso real, com atenção a segurança, desempenho e manutenibilidade.

---

## 💛 Patrocinar o desenvolvimento

Este projeto é mantido de forma independente, fora do horário comercial, com anos de trabalho investidos em segurança, arquitetura e funcionalidades reais em produção. Se ele é útil para você ou para a sua empresa, considere patrocinar o desenvolvimento contínuo.

**[Patrocinar via GitHub Sponsors →](https://github.com/sponsors/allanbarcelos)**

Seu apoio financia:
- Novas funcionalidades e melhorias de segurança
- Manutenção de dependências e compatibilidade com novas versões do .NET
- Documentação e suporte à comunidade
- Implementação de features votadas pelos patrocinadores

---

## Emissão de Nota Fiscal (NFC-e)

A emissão de notas fiscais homologadas **não está incluída nesta versão pública**, mas existe e está em produção em versões dedicadas do sistema. A implementação é feita sob demanda, personalizada para a realidade tributária de cada cliente, e envolve homologação junto à SEFAZ, certificado digital e garantias sobre o correto funcionamento fiscal — portanto é um serviço com custo.

Se você tem interesse, entre em contato: **allan@barcelos.dev**

→ [Documentação técnica completa da implementação de NFC-e](NOTA_FISCAL.md)

---

## PDV em Tablets Android

O sistema suporta operação via **tablets Android em modo kiosk** conectados ao servidor central. O tablet é um thin client — exibe o PDV em WebView fullscreen sem acesso ao Android subjacente. Nenhuma alteração no backend é necessária.

→ [Documentação técnica do cliente Android (arquitetura, provisionamento, hardware)](PDV_ANDROID.md)

---

## O que diferencia esta versão

| Aspecto | Map-OS PHP original | Esta versão (.NET) |
|---------|--------------------|--------------------|
| Runtime | PHP 8 / CodeIgniter 3 | ASP.NET Core 9 |
| Banco | MySQL | PostgreSQL 16 |
| Frontend | Bootstrap + jQuery | Tailwind CSS v3 + Alpine.js |
| Autenticação | Sessão PHP | Cookie Auth + Claims (RBAC) |
| Permissões | Básico | Granular por ação (49 permissões mapeadas) |
| PDV | Não possui | Completo: sessão, maquininha, impressora, fiscal |
| Pagamentos | — | MercadoPago (boleto + Point para cartão presencial) |
| Impressora | — | ESC/POS via socket TCP |
| Auditoria | Básica | Log completo por usuário/IP/ação |
| E-mails | Síncrono | Fila assíncrona com retry (BackgroundService) |
| Segurança | — | Rate limiting, BCrypt, Data Protection, CSRF |
| Infraestrutura | Tradicional | Docker + Nginx + Docker Secrets |
| Exportação | — | Excel (ClosedXML) em todos os relatórios |

---

## Funcionalidades

### Módulos principais

| Módulo | Operações |
|--------|-----------|
| **Clientes / Fornecedores** | CRUD completo, histórico de OS e vendas, cobranças |
| **Ordens de Serviço** | CRUD, produtos/serviços/equipamentos, anotações, anexos, faturamento, impressão |
| **Produtos** | CRUD, controle de estoque, código de barras, marcas e categorias |
| **Serviços** | Tabela de serviços com preço padrão |
| **Vendas** | CRUD, desconto (R$ ou %), faturamento, impressão |
| **Garantias** | Emissão de termos vinculados a OS |
| **Financeiro** | Lançamentos de receitas/despesas, contas bancárias, baixa de títulos |
| **Cobranças** | Geração de boleto via MercadoPago, atualização de status |
| **Arquivos** | Upload e gestão de documentos |
| **Categorias / Contas / Marcas** | CRUD de apoio |
| **Usuários** | Cadastro, expiração de acesso, funções PDV (Operador / Fiscal) |
| **Permissões** | Perfis com controle granular — 49 ações mapeadas |
| **Configurações** | Parâmetros do sistema, tema, impressora térmica, MercadoPago |
| **Emitente** | Dados da empresa para documentos e notas |
| **Auditoria** | Log de ações por usuário, IP e timestamp |
| **E-mails** | Fila de envio com workers automáticos |
| **Pesquisa Global** | Busca unificada de clientes, OS, produtos e vendas |
| **Minha Conta** | Perfil, troca de senha, preferências |

### Relatórios (exportação Excel)

| Relatório | Filtros disponíveis |
|-----------|---------------------|
| Clientes | Tipo (cliente / fornecedor / todos) |
| Produtos | Busca por descrição |
| Serviços | Busca por nome |
| Ordens de Serviço | Período, status |
| Vendas | Período |
| Financeiro | Período, tipo (receita / despesa) |
| SKU — Produtos Vendidos | Período |

### PDV — Ponto de Venda

- **Frente de caixa** completa: busca por código de barras ou descrição, carrinho em tempo real, desconto percentual ou fixo
- **Formas de pagamento:** dinheiro, PIX, débito, crédito, fiado
- **Maquininha MercadoPago Point:** envio direto de cobrança para o dispositivo, polling de status, cancelamento
- **Impressora térmica ESC/POS** via socket TCP: cupom de venda, extrato de sessão, relatório de fechamento
- **Sessões de caixa:** abertura (saldo inicial), fechamento (saldo informado vs. esperado), histórico
- **Pausa e retomada** com validação de senha
- **Autorização Fiscal PDV:** código de cartão criptografado (AES) + PIN (BCrypt) para liberar descontos, remoção de itens e alteração de preço
- **Terminais PDV:** gerenciamento de múltiplos terminais com controle de disponibilidade
- **Atalhos de teclado:** F1–F5 para forma de pagamento, Espaço para finalizar

---

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Framework | ASP.NET Core 9 — Razor Pages |
| ORM | Entity Framework Core 9 (code-first, migrations) |
| Banco de dados | PostgreSQL 16 |
| CSS | Tailwind CSS v3 |
| JS interativo | Alpine.js v3 |
| Ícones | Boxicons 2.1 |
| Autenticação | Cookie Authentication + Claims (RBAC) |
| Hash de senha | BCrypt.Net-Next |
| Criptografia | ASP.NET Core Data Protection (AES) |
| Cache | IMemoryCache (in-process) |
| Excel | ClosedXML 0.104.2 |
| Pagamentos | MercadoPago SDK 2.12.1 |
| Proxy reverso | Nginx 1.27 |
| Containerização | Docker / Docker Compose |

---

## Política de Senhas e PIN Fiscal

### Senha de acesso

| Regra | Comportamento |
|-------|---------------|
| **Primeiro acesso** | Troca obrigatória antes de acessar qualquer página do sistema |
| **Complexidade mínima** | 8 caracteres, ao menos 1 letra maiúscula e 1 número |
| **Alerta de expiração** | Banner visível após 45 dias sem alteração — não bloqueia, mas avisa |
| **Armazenamento** | BCrypt com salt — nunca em texto plano |

Ao criar um usuário, o administrador define uma senha temporária. No primeiro login, o sistema bloqueia a navegação e redireciona para a troca antes de liberar o acesso.

### PIN do Fiscal PDV

| Regra | Comportamento |
|-------|---------------|
| **Primeiro uso do cartão** | Troca de PIN obrigatória antes da autorização ser concluída |
| **Expiração** | PIN com mais de **15 dias** exige renovação no próximo uso |
| **Fluxo no PDV** | Ao detectar PIN expirado, um modal de troca é exibido na frente de caixa sem interromper a operação — após a troca, a autorização é concluída automaticamente |
| **Armazenamento** | BCrypt com salt; código do cartão criptografado com AES |

---

## Segurança

- **Autenticação:** Cookie HttpOnly + SameSite=Lax, expiração deslizante
- **Rate limiting no login:** bloqueio por IP e por e-mail após 5 tentativas (15 min)
- **Permissões verificadas no banco** a cada requisição — nenhum dado sensível no cookie
- **CSRF:** token em todos os formulários e requests AJAX
- **Senhas:** BCrypt com salt; troca obrigatória no primeiro acesso; alerta após 45 dias
- **PIN fiscal PDV:** BCrypt com salt; troca obrigatória no primeiro uso e a cada 15 dias
- **Código fiscal PDV:** criptografado com AES (IDataProtectionProvider), nunca em texto plano
- **Credenciais:** gerenciadas via Docker Secrets, nunca em variáveis de ambiente em texto puro
- **Auditoria completa:** login, CRUD, operações PDV e autorizações fiscais registrados com IP e timestamp

---

## Pré-requisitos

**Desenvolvimento local:**
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) — para compilar o Tailwind CSS
- PostgreSQL 16+

**Via Docker (recomendado):**
- Docker Engine 24+
- Docker Compose v2

---

## Início rápido

### Com Docker

```bash
# 1. Clone o repositório
git clone https://github.com/allanbarcelos/mapos-dotnet.git
cd mapos-dotnet

# 2. Configure o ambiente
cp .env.example .env
# Edite .env com suas senhas

# 3. Gere os Docker secrets
make setup

# 4. Suba a stack
make up
```

Acesse **http://localhost**

Login padrão criado no primeiro boot:
- **E-mail:** `admin@mail.com`
- **Senha:** valor definido em `ADMIN_PASSWORD` no `.env`

---

### Desenvolvimento local (sem Docker)

```bash
# 1. Instale dependências Node
npm install

# 2. Compile o CSS
npm run build:css

# 3. Configure a connection string
# appsettings.Development.json ou variável de ambiente:
export ConnectionStrings__DefaultConnection="Host=localhost;Database=mapos;Username=postgres;Password=postgres"

# 4. Configure a senha do admin (primeiro boot)
export MAPOS_ADMIN_PASSWORD="sua_senha"

# 5. Execute (migrations rodam automaticamente)
dotnet run
```

---

## Variáveis de ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | Connection string Npgsql completa |
| `MAPOS_ADMIN_PASSWORD` | Senha do usuário admin criado no seed inicial |
| `ASPNETCORE_ENVIRONMENT` | `Development` ou `Production` |

Em produção com Docker, os valores sensíveis são lidos de **Docker secrets** pelo `entrypoint.sh` — nunca de variáveis de ambiente em texto puro.

---

## Secrets (Docker)

| Arquivo | Conteúdo |
|---------|----------|
| `secrets/postgres_connection` | Connection string completa |
| `secrets/postgres_password` | Senha do PostgreSQL |
| `secrets/admin_password` | Senha do usuário admin |
| `secrets/smtp_host` | Servidor SMTP |
| `secrets/smtp_port` | Porta SMTP |
| `secrets/smtp_ssl` | TLS habilitado (`true` / `false`) |
| `secrets/smtp_username` | Usuário SMTP |
| `secrets/smtp_password` | Senha SMTP |
| `secrets/smtp_from` | Endereço de remetente |
| `secrets/mp_access_token` | Access Token MercadoPago |
| `secrets/mp_boleto_expiration` | Expiração do boleto (ISO 8601, ex: `P3D`) |

```bash
make setup   # gera todos os arquivos de secrets a partir do .env
```

---

## Fila de e-mails

O envio de e-mails funciona de forma assíncrona via fila no banco — nenhuma requisição HTTP aguarda o SMTP.

| Worker | Intervalo | Responsabilidade |
|--------|-----------|-----------------|
| `EmailProcessWorker` | 2 minutos | Envia e-mails com status `pending` |
| `EmailRetryWorker` | 5 minutos | Recoloca na fila os `failed` e reseta os `sending` travados |

```
pending → sending → sent
                 ↘ failed → pending (retry)
```

---

## Comandos úteis

```bash
make up            # sobe a stack com build
make up-d          # sobe em background
make down          # para os containers
make down-v        # para e apaga os volumes (limpa o banco)
make logs          # acompanha os logs

make psql          # abre o psql no container db

make migration NAME=NomeDaMigration   # cria nova migration
make db-update                        # aplica migrations pendentes

make build         # dotnet build local
make run           # dotnet run local
make css           # compila Tailwind CSS
make css-watch     # Tailwind em modo watch
```

---

## Estrutura do projeto

```
mapos-dotnet/
├── Data/
│   └── ApplicationDbContext.cs     # DbContext com 29 entidades mapeadas
├── Migrations/                     # Migrations EF Core
├── Models/                         # 29 entidades de domínio
├── Pages/                          # Razor Pages por módulo
│   ├── Account/                    # Login, Logout
│   ├── Auditoria/
│   ├── Clientes/
│   ├── Cobrancas/
│   ├── Configuracoes/
│   ├── Emails/
│   ├── Emitente/
│   ├── Financeiro/
│   ├── Garantias/
│   ├── MinhaConta/
│   ├── Os/
│   ├── Pdv/                        # Frente de caixa, terminais, sessões
│   ├── Permissoes/
│   ├── Produtos/
│   ├── Relatorios/                 # 7 relatórios com exportação Excel
│   ├── Servicos/
│   ├── Usuarios/
│   ├── Vendas/
│   └── Shared/                     # Layout, sidebar, alertas, partials
├── Services/
│   ├── PermissaoService.cs         # RBAC com cache (IMemoryCache)
│   ├── ConfiguracaoService.cs      # Configs com cache
│   ├── AuditService.cs
│   ├── LoginThrottleService.cs     # Rate limiting no login
│   ├── FiscalCodigoProtector.cs    # Criptografia AES para código fiscal
│   ├── MercadoPagoService.cs       # Boleto e status
│   ├── PdvPrinterService.cs        # ESC/POS via socket TCP
│   ├── PdvPointService.cs          # MercadoPago Point (maquininha)
│   ├── ExcelHelper.cs              # Geração de Excel (ClosedXML)
│   ├── EmailQueueService.cs        # Workers de envio de e-mail
│   └── ...
├── wwwroot/
│   └── css/
│       ├── input.css               # Source Tailwind
│       └── app.css                 # Build gerado (não versionado)
├── Dockerfile
├── docker-compose.yml
├── nginx.conf
├── entrypoint.sh
└── Makefile
```

---

## Créditos

Este projeto é inspirado no **[Map-OS](https://github.com/RamonSilva20/mapos)**, criado por **[Ramon Silva](https://github.com/RamonSilva20)** e mantido pela comunidade open source. O crédito pela concepção original do sistema, modelagem de negócio e filosofia do produto pertence ao projeto original e seus contribuidores.

Esta implementação é independente, remodelada integralmente em ASP.NET Core com arquitetura, banco de dados e funcionalidades próprias.

- Repositório original: https://github.com/RamonSilva20/mapos
- Licença original: MIT

---

## Licença

MIT
