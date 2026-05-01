# Avaliação e Plano de Melhorias — Map-OS .NET

> Avaliação inicial: 2026-05-01

---

## Status geral

O projeto está bem estruturado, com boas práticas de ASP.NET Core 9 (async/await, DI, Razor Pages, EF Core). Todos os pontos críticos identificados foram corrigidos. O sistema está em produção ativa.

---

## ✅ Já implementado

### Críticos (todos resolvidos)

- **C1 — Credenciais no repositório:** `appsettings.json` limpo; credenciais gerenciadas exclusivamente via Docker Secrets e `appsettings.Development.json` (não versionado).
- **C2 — FiscalPdvCodigo em texto plano:** código do cartão fiscal criptografado com AES via `IDataProtectionProvider` (`FiscalCodigoProtector`). Coluna `fiscal_pdv_codigo` armazena ciphertext; descriptografia ocorre apenas na exibição do cartão.
- **C3 — Sem rate limiting no login:** `LoginThrottleService` implementado — bloqueio por IP e por e-mail após 5 tentativas falhas, com cooldown de 15 minutos via `IMemoryCache`.
- **C4 — Lançamento financeiro duplicado / race condition no PDV:** transação `IDbContextTransaction` envolve criação de venda + itens + estoque + lançamento. Índice único em `lancamentos.venda_id` (`WHERE venda_id IS NOT NULL`) impede duplicatas. IDOR nas sessões PDV corrigido com verificação de propriedade (`s.OperadorId == UsuarioId`).
- **C5 — `permissao_id` lido do cookie sem validação:** `PermissaoId` resolvido do banco via `UsuarioId` (claim assinado pelo servidor), com cache de 5 minutos no `IMemoryCache`. Claim removido do cookie.

### Moderados (parcialmente resolvidos)

- **M1 — `Cobranca.Total` como `string`:** alterado para `decimal?`. Migration gerada com `USING total::numeric(10,2)` para conversão no PostgreSQL. Views e PageModels ajustados (`HasValue ? ToString("N2") : "-"`).
- **M2 — Índices ausentes:** migration `IndicesEMaxLength` criada com índices em `clientes.documento`, `usuarios.email`, `sessoes_caixa.(operador_id, status)` e `vendas.data_venda`.
- **M3 — Campos sem `[MaxLength]`:** `[MaxLength]` aplicado nos campos `Defeito`, `Observacoes`, `LaudoTecnico`, `DescricaoProduto`, `Solucao` e outros. Coluna `fiscal_pdv_codigo` ampliada para `varchar(500)` para comportar o ciphertext AES.

### Outros

- **Vulnerabilidades NuGet:** pacotes transitivos `System.Text.Encodings.Web 4.5.0` e `System.Security.Cryptography.Xml` com CVEs conhecidos foram substituídos por versões seguras via pin explícito no `.csproj`. `Microsoft.AspNetCore.Authentication.Cookies 2.2.0` (legado) removido — a autenticação por cookie está incluída no SDK do .NET 9.
- **Desconto incorreto no PDV:** cálculo do `Lancamento` corrigido — `Valor` agora recebe o subtotal antes do desconto, `ValorDesconto` recebe o total final pago.

---

## 🟡 Pendente — Moderados

### M4 — Email sem validação de conectividade SMTP

**Problema:** `EmailQueueService` processa a fila sem testar a conexão SMTP. Falhas silenciosas em envios críticos (cobranças, confirmações de OS).

**Arquivos afetados:** `Services/EmailQueueService.cs`, `Program.cs`

**Passos:**
1. Adicionar `TestarConexaoAsync()` — handshake SMTP sem envio, logando aviso se falhar (sem lançar exceção)
2. Implementar retry com backoff exponencial: imediata → 2 min → 10 min → marcar como `failed`
3. Adicionar campos `tentativas` e `proxima_tentativa` na tabela de fila se não existirem
4. Registrar falhas definitivas na auditoria

---

### M5 — Sem validação de complexidade de senha

**Problema:** mínimo atual de 6 caracteres é insuficiente para um sistema de negócios.

**Regra proposta:** mínimo 8 caracteres, ao menos 1 letra maiúscula, ao menos 1 número.

**Arquivos afetados:** `Pages/Usuarios/Adicionar.cshtml.cs`, `Pages/Usuarios/Editar.cshtml.cs`, `Pages/MinhaConta/*.cshtml.cs`

**Passos:**
1. Criar atributo `[SenhaForte]` ou usar `[RegularExpression]` com regex adequada
2. Aplicar nos InputModels de criação e edição de usuário
3. Adicionar mensagem de erro clara nas views
4. Verificar tela de troca de senha em `MinhaConta`

---

## 🟢 Pendente — Menores

### N1 — UI de consulta de auditoria

**Problema:** logs são gravados mas `/Auditoria` não tem busca, filtros por data/usuário, nem exportação.

**Passos:**
1. Filtros em `Pages/Auditoria/Index.cshtml.cs`: data início/fim, usuário, texto livre
2. Paginação (já existe padrão no projeto)
3. Botão "Exportar Excel" via `ExcelHelper` (padrão dos relatórios)

---

### N2 — Alerta de estoque mínimo

**Problema:** campo `EstoqueMinimo` existe em `Produto` mas nunca aciona notificação.

**Passos:**
1. Adicionar badge no dashboard quando `Estoque <= EstoqueMinimo` (somente para quem tem `vProduto`)
2. Query no `Pages/Index.cshtml.cs`: `ProdutosAbaixoMinimo = await Db.Produtos.CountAsync(p => p.Estoque <= p.EstoqueMinimo && p.EstoqueMinimo > 0)`
3. Link para `/Produtos?filtro=estoque_baixo`
4. (Opcional) Email diário com lista crítica via `EmailQueueService`

---

### N3 — Validação de status de OS

**Problema:** `Os.Status` é string livre — qualquer valor pode ser salvo, causando inconsistências nos relatórios e filtros.

**Passos:**
1. Validar no `OnPostAsync` dos PageModels de OS que o status existe na lista configurada no sistema
2. Substituir inputs de texto livre por `<select>` carregado da configuração
3. (Opcional) Check constraint no banco via migration

---

### N4 — `PaginationHelper` para eliminar duplicação

**Problema:** lógica de paginação (`skip`, `take`, `totalPages`) duplicada em 10+ PageModels.

**Passos:**
1. Criar `Services/PaginationHelper.cs` com método estático `Paginar<T>(IQueryable<T> query, int page, int perPage)`
2. Substituir código duplicado nos PageModels de listagem

---

### N5 — Inconsistência de tipos em `Quantidade`

**Problema:** `ServicoOs.Quantidade` é `double` enquanto `ProdutoOs.Quantidade` é `int` — campos análogos com tipos diferentes.

**Passos:**
1. Avaliar se serviços precisam de quantidade fracionada (ex: 0,5 hora de serviço)
2. Se não: `double` → `int` + migration
3. Se sim: `ProdutoOs.Quantidade` → `decimal` para consistência + migration

---

### N6 — Soft-delete (`IsDeleted`)

**Problema:** exclusões são físicas — sem conformidade com LGPD para rastreabilidade e possibilidade de recuperação.

**Passos:**
1. Adicionar `IsDeleted bool` + `DeletedAt DateTime?` nos modelos principais (`Cliente`, `Produto`, `Os`, `Venda`, `Usuario`)
2. `HasQueryFilter(e => !e.IsDeleted)` no `ApplicationDbContext`
3. Converter `Db.X.Remove()` em `x.IsDeleted = true; x.DeletedAt = DateTime.UtcNow`
4. Migration com colunas novas (padrão `false`)
5. Rota de restauração para admins

---

### N7 — Status de pagamento hardcoded no `MercadoPagoService`

**Problema:** strings de status (`"approved"`, `"rejected"`, etc.) hardcoded dificultam a adição de outros gateways.

**Passos:**
1. Criar enum `StatusPagamento { Aprovado, Recusado, Pendente, Cancelado, Erro }`
2. Criar interface `IGatewayPagamento` com método `Task<ResultadoPagamento> ConsultarAsync(string id)`
3. Refatorar `MercadoPagoService` para implementar a interface
4. `PdvPointService` passa a retornar `ResultadoPagamento` com enum, não string bruta

---

## Ordem de execução recomendada para o que resta

```
Semana 1
  M5  Validação de complexidade de senha
  M4  Retry e teste de conectividade SMTP

Semana 2 — Melhorias de UX e integridade
  N1  UI de auditoria com filtros e export Excel
  N2  Alerta de estoque mínimo no dashboard
  N3  Validação de status de OS

Semana 3 — Refatoração técnica
  N4  PaginationHelper
  N5  Consistência de tipos em Quantidade
  N7  Interface IGatewayPagamento

Semana 4 — Soft-delete (maior impacto)
  N6  IsDeleted em todos os modelos principais
```

---

## Itens de infra/ops (fora do escopo de código)

- Certificado SSL no `nginx.conf` com domínio real
- Backup automático do PostgreSQL (job no `docker-compose`)
- Rotação de Docker secrets antes de cada deploy
- `ASPNETCORE_ENVIRONMENT=Production`
- Regras de firewall (expor somente portas 80 e 443)
- Teste da integração MercadoPago em sandbox antes de ativar produção

---

## Pontos fortes (mantidos)

- BCrypt para hash de senhas e PINs
- Cookie HttpOnly + SameSite=Lax
- `[Authorize]` no `PageModelBase` — todas as páginas protegidas por padrão
- Zero risco de SQL injection — 100% LINQ parametrizado
- Auditoria completa de operações sensíveis com IP e timestamp
- PDV com terminais, pausas cronometradas, cartão fiscal AES, ESC/POS via TCP
- Permissões granulares por módulo (49 ações mapeadas)
- Docker multi-stage com Docker Secrets
- Exportação Excel em todos os relatórios (ClosedXML)
- Integração MercadoPago (boleto e Point para cartão presencial)
- Migrations incrementais com Up/Down
