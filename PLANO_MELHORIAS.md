# Plano de Melhorias — Map-OS

> Gerado em 2026-05-01
> Continuação de AVALIACAO.md — todos os 5 críticos já foram resolvidos.

---

## 🟡 Moderados (M1–M5)

### M1 — `Cobranca.Total` como `string` → `decimal`

**Problema:** campo financeiro armazenado como texto impossibilita cálculos diretos e reconciliação.

**Arquivos afetados:**
- `Models/Cobranca.cs`
- `Services/MercadoPagoService.cs`
- `Pages/Cobrancas/*.cshtml` e `.cshtml.cs`
- Nova migration EF Core

**Passos:**
1. Alterar `Cobranca.Total` de `string` para `decimal` em `Models/Cobranca.cs`
2. Ajustar `MercadoPagoService` onde lê/escreve o campo (parse de string → decimal direto)
3. Ajustar views e PageModels que exibem ou calculam com `Total`
4. Gerar migration: converter coluna `total` de `text` para `numeric(10,2)` com `USING total::numeric`
5. Testar integração com MercadoPago em sandbox

---

### M2 — Índices ausentes no banco

**Problema:** consultas frequentes sem índice causam full table scan, impacto em performance com volume de dados.

**Índices a criar:**

| Coluna | Tabela | Motivo |
|--------|--------|--------|
| `email` | `usuarios` | Toda autenticação faz `WHERE email = ?` |
| `data_venda` | `vendas` | Filtros de data em relatórios e dashboard |
| `(operador_id, status)` | `sessoes_caixa` | Verificação de sessão aberta no PDV a cada request |
| `documento` | `clientes` | Busca por CPF/CNPJ em cadastros e OS |

**Passos:**
1. Criar migration `AddPerformanceIndexes`
2. Adicionar os 4 índices via `migrationBuilder.CreateIndex`
3. Refletir em `ApplicationDbContext` com `.HasIndex()`

---

### M3 — Campos sem `[MaxLength]`

**Problema:** campos de texto ilimitado — risco de bloat no banco e potencial DoS.

**Campos a limitar:**

| Campo | Modelo | Limite sugerido |
|-------|--------|----------------|
| `Defeito` | `Os.cs` | 2000 |
| `Observacoes` | `Os.cs`, `Venda.cs`, `Lancamento.cs` | 2000 |
| `DescricaoProduto` | `Produto.cs` | 500 |
| `Solucao` | `Os.cs` | 2000 |
| `PdvReceiptHeader/Footer` | `Configuracao` (via serviço) | 500 |
| `FiscalPdvCodigo` | `Usuario.cs` | 500 |

**Passos:**
1. Adicionar `[MaxLength(N)]` nos modelos listados
2. Gerar migration para aplicar `varchar(N)` no banco
3. Adicionar validação de comprimento nas views correspondentes (já propagado pelo TagHelper `asp-for`)

---

### M4 — Email sem validação de conectividade SMTP

**Problema:** `EmailQueueService` processa a fila sem testar conexão SMTP. Falhas silenciosas não são sinalizadas.

**Arquivos afetados:**
- `Services/EmailQueueService.cs` (ou equivalente)
- `Program.cs` (registro do hosted service)

**Passos:**
1. Adicionar método `TestarConexaoAsync()` no serviço de email que faz handshake SMTP sem enviar mensagem
2. Chamar o teste na inicialização do hosted service (logar aviso se falhar, não lançar exceção)
3. Implementar retry com backoff exponencial no processamento da fila: 1ª tentativa imediata → 2ª após 2 min → 3ª após 10 min → marcar como falha
4. Adicionar campo `tentativas` e `proxima_tentativa` na tabela de fila de emails (se não existir)
5. Registrar falhas definitivas na auditoria

---

### M5 — Sem validação de complexidade de senha

**Problema:** mínimo de 6 caracteres é insuficiente para um sistema de negócios.

**Regra proposta:** mínimo 8 caracteres, ao menos 1 letra maiúscula, ao menos 1 número.

**Arquivos afetados:**
- `Pages/Account/Login.cshtml.cs` (não — só valida na criação)
- `Pages/Usuarios/Adicionar.cshtml.cs`
- `Pages/Usuarios/Editar.cshtml.cs`
- `Pages/MinhaConta/*.cshtml.cs` (troca de senha)

**Passos:**
1. Criar atributo de validação customizado `[SenhaForte]` ou usar `[RegularExpression]` com regex adequada
2. Aplicar nos InputModels de criação e edição de usuário
3. Adicionar mensagem de erro clara na view
4. Verificar se a tela de troca de senha (MinhaConta) também aplica a regra

---

## 🟢 Menores (N1–N7)

### N1 — UI de consulta de auditoria

**Problema:** logs são gravados mas a página `/Auditoria` não tem busca, filtro por data/usuário, nem exportação.

**Passos:**
1. Adicionar filtros na `Pages/Auditoria/Index.cshtml.cs`: data início/fim, usuário, texto livre
2. Paginação (já existe padrão no projeto)
3. Botão "Exportar Excel" usando `ExcelHelper.Gerar` (padrão dos relatórios)

---

### N2 — Alerta de estoque mínimo

**Problema:** campo `EstoqueMinimo` existe em `Produto` mas nunca aciona notificação.

**Passos:**
1. Adicionar card/badge no dashboard quando `Estoque <= EstoqueMinimo` (somente para quem tem `vProduto`)
2. Criar query no `Pages/Index.cshtml.cs`: `ProdutosAbaixoMinimo = await Db.Produtos.CountAsync(p => p.Estoque <= p.EstoqueMinimo && p.EstoqueMinimo > 0)`
3. Exibir alerta visual com link para `/Produtos?filtro=estoque_baixo`
4. (Opcional) Enviar email diário com lista de produtos em estoque crítico via `EmailQueueService`

---

### N3 — Validação de status de OS contra lista configurada

**Problema:** `Os.Status` é `string` livre — é possível salvar qualquer valor sem validação, causando inconsistências.

**Passos:**
1. No `OnPostAsync` dos PageModels de OS, validar que o status informado existe em `OsStatusList` (configuração do sistema)
2. Substituir inputs de texto livre por `<select>` carregado dinamicamente nos formulários de OS
3. Adicionar check constraint no banco via migration (opcional, mais rígido)

---

### N4 — `PaginationHelper` para eliminar duplicação

**Problema:** lógica de paginação (`skip`, `take`, `totalPages`) repetida em 10+ PageModels.

**Passos:**
1. Criar `Services/PaginationHelper.cs` com método estático `Paginar<T>(IQueryable<T> query, int page, int perPage)`
2. Substituir o código duplicado nos PageModels de listagem (`Clientes`, `Produtos`, `Os`, `Vendas`, etc.)

---

### N5 — `ServicoOs.Quantidade` como `double`

**Problema:** `ServicoOs.Quantidade` é `double` enquanto `ProdutoOs.Quantidade` é `int` — inconsistência de tipos para campos análogos.

**Passos:**
1. Avaliar se serviços precisam de quantidade fracionada (ex: 0,5 hora)
2. Se não: alterar `double` → `int` + migration
3. Se sim: alterar `ProdutoOs.Quantidade` para `decimal` para consistência + migration

---

### N6 — Soft-delete (`IsDeleted`)

**Problema:** exclusões são físicas — dado sumido é dado perdido, sem conformidade com LGPD para auditabilidade.

**Passos:**
1. Adicionar `IsDeleted bool` + `DeletedAt DateTime?` nos modelos principais (`Cliente`, `Produto`, `Os`, `Venda`, `Usuario`)
2. Configurar `HasQueryFilter(e => !e.IsDeleted)` no `ApplicationDbContext` para transparência automática
3. Converter `Db.X.Remove()` para `x.IsDeleted = true; x.DeletedAt = DateTime.UtcNow` nos PageModels
4. Adicionar migration com as novas colunas (valor padrão `false`)
5. Criar rota de restauração para admins (ex: `POST /Clientes/Restaurar/{id}`)

---

### N7 — Status de pagamento hardcoded no `MercadoPagoService`

**Problema:** strings de status (`"approved"`, `"rejected"`, etc.) hardcoded dificultam adição de outros gateways.

**Passos:**
1. Criar enum `StatusPagamento { Aprovado, Recusado, Pendente, Cancelado, Erro }`
2. Criar interface `IGatewayPagamento` com método `Task<ResultadoPagamento> ConsultarAsync(string id)`
3. Refatorar `MercadoPagoService` para implementar a interface e mapear os status internos para o enum
4. `PdvPointService` passa a retornar `ResultadoPagamento` com o enum ao invés de string bruta

---

## Ordem de execução recomendada

```
Semana 1 — Moderados de banco (sem risco de regressão alto)
  M2  Índices de performance       ← sem breaking change, só migration
  M3  [MaxLength] nos modelos      ← migration simples, melhora qualidade
  M1  Cobranca.Total → decimal     ← migration + ajuste no MercadoPago

Semana 2 — Moderados de lógica
  M5  Validação de complexidade de senha
  M4  Retry e teste SMTP

Semana 3 — Melhorias de UX e integridade
  N1  UI de auditoria com filtros e export
  N2  Alerta de estoque mínimo
  N3  Validação de status OS

Semana 4 — Refatoração técnica
  N4  PaginationHelper
  N5  Consistência de tipos (Quantidade)
  N7  Interface IGatewayPagamento

Semana 5 — Soft-delete (maior impacto)
  N6  IsDeleted em todos os modelos principais
```

---

## Itens fora do escopo deste plano (infra/ops)

Estes itens dependem de configuração de ambiente, não de código:

- Certificado SSL no `nginx.conf` com domínio real
- Backup automático do PostgreSQL (job no `docker-compose`)
- Rotação de Docker secrets antes do deploy
- `ASPNETCORE_ENVIRONMENT=Production`
- Regras de firewall (somente portas 80 e 443)
- Teste da integração MercadoPago em sandbox
