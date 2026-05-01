# Avaliação do Projeto Map-OS — Estado Atual

> Gerado em 2026-05-01

---

## Resumo Executivo

O projeto está bem estruturado, com boas práticas de ASP.NET Core 9 (async/await, DI, Razor Pages, EF Core). **Está pronto para staging, mas NÃO está pronto para produção** sem resolver os problemas críticos listados abaixo.

- **Framework:** ASP.NET Core 9 + Razor Pages
- **Banco de dados:** PostgreSQL 16 via EF Core 9 (Npgsql)
- **Frontend:** Tailwind CSS v3, Alpine.js
- **Infraestrutura:** Docker (multi-stage), Nginx, docker-compose
- **Módulos:** Clientes, Produtos, Serviços, OS, Vendas, Financeiro, PDV, Usuários, Permissões, Relatórios, Cobranças, Garantias, Configurações

---

## 🔴 Críticos — corrigir antes de produção

### 1. Credenciais hardcoded no `appsettings.json`
Senha do PostgreSQL em texto plano no repositório. Qualquer pessoa com acesso ao código tem acesso ao banco.

**Fix:** Remover credenciais do arquivo e usar exclusivamente Docker secrets ou variáveis de ambiente.

---

### 2. `FiscalPdvCodigo` armazenado em texto plano (`Models/Usuario.cs`)
O PIN fiscal (`FiscalPdvPin`) está corretamente hasheado com BCrypt, mas o código do cartão fiscal (`FiscalPdvCodigo`) está em texto plano. Um vazamento do banco expõe todos os cartões fiscais.

**Fix:** Hashear `FiscalPdvCodigo` com BCrypt da mesma forma que o PIN. Exige migration para re-hash dos registros existentes.

---

### 3. Sem rate limiting no login (`Pages/Account/Login.cshtml.cs`)
Nenhum mecanismo de bloqueio contra tentativas repetidas de autenticação. Ataque de força bruta totalmente irrestrito.

**Fix:** Implementar lockout progressivo — ex: 5 tentativas falhas → bloqueio de 15 min por IP e/ou por conta.

---

### 4. Risco de lançamento financeiro duplicado (`Pages/Pdv/Index.cshtml.cs`)
O PDV cria um `Lancamento` automaticamente ao fechar uma venda, mas não há constraint único de `(VendaId, Tipo)` na tabela `lancamentos`. Um lançamento manual subsequente gera duplicidade de receita sem nenhum aviso.

**Fix:** Adicionar constraint único em `(venda_id)` na tabela `lancamentos` e garantir que a relação `Venda.LancamentoId` seja respeitada como chave.

---

### 5. `permissao_id` no cookie sem verificação adicional (`Pages/Account/Login.cshtml.cs`)
O claim `permissao_id` é gravado no cookie de sessão em texto plano. Um usuário que manipule o cookie pode declarar um perfil de permissão diferente do seu.

**Fix:** Revalidar o `permissao_id` contra o banco a cada requisição (via `IPermissaoService`) ou assinar o claim com HMAC separado do cookie principal.

---

## 🟡 Moderados — próxima sprint

### 6. `Cobranca.Total` como `string` (`Models/Cobranca.cs:16`)
Campo financeiro tipado como `string` impossibilita cálculos diretos e reconciliação de pagamentos.

**Fix:** Alterar para `decimal`. Exige migration + ajuste no `MercadoPagoService`.

---

### 7. Índices ausentes no banco
Consultas frequentes sem índice de suporte:

| Coluna | Tabela | Uso |
|--------|--------|-----|
| `email` | `usuarios` | Login (toda autenticação) |
| `data_venda` | `vendas` | Filtros de data em relatórios |
| `(operador_id, status)` | `sessoes_caixa` | Verificação de sessão aberta no PDV |
| `documento` | `clientes` | Busca por CPF/CNPJ |

**Fix:** Migration adicionando os índices.

---

### 8. Campos de texto sem `[MaxLength]`
`Defeito`, `Observacoes`, `DescricaoProduto` (e outros) aceitam texto de tamanho ilimitado — risco de bloat no banco e potencial DoS.

**Fix:** Aplicar `[MaxLength]` adequado em cada campo (ex: 2000 chars para observações, 500 para defeito).

---

### 9. Email enviado sem validação de conectividade SMTP
`EmailQueueService` processa a fila sem testar a conexão SMTP primeiro. Falhas silenciosas em comunicações críticas (confirmações de OS, cobranças).

**Fix:** Adicionar teste de conectividade na inicialização do worker e retry com backoff exponencial.

---

### 10. Sem validação de complexidade de senha
Mínimo atual de 6 caracteres é insuficiente para um sistema de negócios.

**Fix:** Exigir mínimo de 8 caracteres com ao menos 1 número e 1 letra maiúscula.

---

## 🟢 Menores / Melhorias de qualidade

| Item | Arquivo(s) | Impacto |
|------|-----------|---------|
| Lógica de paginação duplicada em 10+ PageModels | `*/Index.cshtml.cs` | Manutenibilidade |
| `ServicoOs.Quantidade` como `double` vs `ProdutoOs.Quantidade` como `int` | `Models/ServicoOs.cs`, `Models/ProdutoOs.cs` | Inconsistência de tipos |
| `ValorDesconto` armazenado (deveria ser calculado) | `Models/Lancamento.cs`, `Models/Os.cs` | Risco de dados inconsistentes |
| Sem soft-delete (`IsDeleted`) | Modelos em geral | Conformidade com LGPD (mencionada no próprio sistema) |
| Sem UI para consulta da auditoria | `Pages/Auditoria/` | Logs gravados mas não consultáveis |
| Sem alerta de estoque mínimo | `Models/Produto.cs` | Campo `EstoqueMinimo` existe mas não aciona nada |
| Status de OS como `string` livre sem validação contra lista configurada | `Models/Os.cs:11` | Integridade de dados |
| Status de pagamento hardcoded no `MercadoPagoService` | `Services/MercadoPagoService.cs:229` | Difícil adicionar outros gateways |

---

## ✅ Pontos Fortes

- BCrypt para hash de senhas e PINs
- Cookie HttpOnly + SameSite=Lax configurado
- `[Authorize]` no `PageModelBase` — todas as páginas protegidas por padrão
- Auditoria de operações sensíveis via `IAuditService`
- Zero risco de SQL injection — 100% LINQ parametrizado (sem `FromSqlRaw`)
- PDV bem arquitetado: terminais, pausas cronometradas, fiscal com cartão, ESC/POS via TCP
- Migrations incrementais com Up/Down
- Docker multi-stage com Docker secrets (credenciais não em variáveis de ambiente)
- Permissões granulares por módulo (`vCliente`, `aCliente`, `eCliente`, `dCliente`)
- Dashboard e relatórios com visibilidade controlada por permissão
- Exportação de relatórios para Excel (ClosedXML)
- Integração MercadoPago (pagamentos e Point para cartão presencial)
- LGPD mencionada explicitamente na UI de cadastro de usuários

---

## Funcionalidades ausentes / gaps

| Funcionalidade | Status | Observação |
|----------------|--------|------------|
| 2FA (autenticação em dois fatores) | Não implementado | Somente senha |
| UI de consulta de auditoria | Parcial | Logs gravados, sem tela de busca |
| Backup automático do PostgreSQL | Não implementado | Nenhum job no docker-compose |
| Alerta de estoque mínimo | Não implementado | Campo existe, sem notificação |
| Template engine para emails | Não implementado | HTML montado manualmente no serviço |
| Webhook MercadoPago (retorno automático) | Parcial | Callbacks não identificados |
| Reconciliação de pagamentos automática | Parcial | `Cobranca` atualizada manualmente |
| Rate limiting / proteção contra brute-force | Não implementado | **Crítico** |
| Política de retenção de dados (LGPD) | Não implementado | Sem expiração/exclusão automatizada |

---

## Dívida técnica

| Item | Esforço estimado | Impacto |
|------|-----------------|---------|
| Criar `PaginationHelper` para eliminar duplicação | 0,5 dia | Manutenibilidade |
| Extrair repository pattern (desacoplar PageModels do DbContext) | 2 dias | Testabilidade |
| Template engine para emails (ex: Scriban) | 1 dia | UX de comunicação |
| UI completa de auditoria (filtros, export) | 2 dias | Conformidade/compliance |
| Reconciliação automática de pagamentos | 2 dias | Operacional |
| Adicionar API REST (para possível app mobile) | 3 dias | Extensibilidade futura |

---

## Checklist de produção

```
[ ] Remover credenciais do appsettings.json
[ ] Hashear FiscalPdvCodigo (migration + re-hash dos registros)
[ ] Implementar rate limiting no login
[ ] Adicionar índices ausentes (migration)
[ ] Corrigir Cobranca.Total para decimal (migration)
[ ] Configurar certificado SSL no nginx.conf com domínio real
[ ] Testar envio de email com SMTP de produção
[ ] Configurar backup automático do PostgreSQL
[ ] Revisar e rotacionar todos os Docker secrets antes do deploy
[ ] ASPNETCORE_ENVIRONMENT=Production
[ ] Verificar regras de firewall (expor somente portas 80 e 443)
[ ] Testar integração MercadoPago em sandbox antes de ativar produção
```

---

## Conclusão

A arquitetura é sólida e o código é consistente. Os módulos de PDV e relatórios são particularmente bem implementados. Os **5 itens críticos** são todos corrigíveis em 1–2 dias de trabalho e não exigem refatoração profunda.

O maior risco operacional imediato é a **ausência de rate limiting no login** (ataque de força bruta) e o **`FiscalPdvCodigo` em texto plano** (exposição de credenciais fiscais em caso de vazamento do banco).
