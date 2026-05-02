# Plano de Testes — Map-OS .NET

Cobertura completa com testes unitários e de integração para o sistema ASP.NET Core 9.

---

## Estrutura de projetos

```
mapos-dotnet.sln
mapos-dotnet/                             ← projeto da aplicação
tests/
  mapos-dotnet.Unit.Tests/
    mapos-dotnet.Unit.Tests.csproj
    Services/
      LoginThrottleServiceTests.cs
      PermissaoServiceTests.cs
      FiscalCodigoProtectorTests.cs
      ConfiguracaoServiceTests.cs
      MercadoPagoServiceTests.cs
      EscPosServiceTests.cs
      EmailQueueServiceTests.cs
    Pages/
      PageModelBaseTests.cs
      Account/
        LoginTests.cs
      Pdv/
        PdvFinalizarVendaTests.cs
        PdvAutorizarFiscalTests.cs
        PdvPausarRetomarTests.cs
        AbrirCaixaTests.cs
        FecharCaixaTests.cs
    Helpers/
      InMemoryDbFactory.cs
      MockFactory.cs
      ClaimsPrincipalFactory.cs
      PageModelTestHelper.cs
      EphemeralDataProtection.cs
  mapos-dotnet.Integration.Tests/
    mapos-dotnet.Integration.Tests.csproj
    Infrastructure/
      PostgresFixture.cs
      SeedData.cs
      AppFactory.cs
    Pdv/
      PdvFinalizarVendaIntegrationTests.cs
      AbrirFecharCaixaIntegrationTests.cs
    Auth/
      LoginIntegrationTests.cs
    Permissoes/
      PermissaoServiceIntegrationTests.cs
    Email/
      EmailQueueIntegrationTests.cs
```

---

## Convenção de nomenclatura

Todos os métodos de teste seguem o padrão:

```
MetodoTestado_Cenario_ComportamentoEsperado
```

Exemplos:
- `RegistrarFalha_CincoFalhasConsecutivas_BloqueiaChave`
- `OnPostFinalizarVendaAsync_CarrinhoVazio_RetornaErroJson`
- `GetPermissoesAsync_JsonComValorNumerico_ParseaComoBoolean`

---

## Dependências

### Projeto de testes unitários

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>mapos_dotnet.Unit.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../../mapos-dotnet/mapos-dotnet.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Moq" Version="4.20.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="9.0.*" />
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.*" />
  </ItemGroup>
</Project>
```

### Projeto de testes de integração

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>mapos_dotnet.Integration.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../../mapos-dotnet/mapos-dotnet.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Moq" Version="4.20.*" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.10.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.*" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.*" />
  </ItemGroup>
</Project>
```

---

## Unitário vs Integração

| Tipo | Critério |
|------|----------|
| **Unitário** | Zero I/O externo. DbContext via InMemory, DataProtection via `EphemeralDataProtectionProvider`, cache via `MemoryCache` real. Roda em millisegundos, sem Docker. |
| **Integração** | Requer semântica SQL real: índices únicos parciais, transações com rollback parcial, constraints de FK. Usa Testcontainers + `Database.Migrate()` — nunca `EnsureCreated`. |

> **Regra**: se o comportamento que está sendo testado depende de um índice `UNIQUE` ou de uma `FK` do PostgreSQL, é teste de integração. Caso contrário, é unitário.

---

## Infraestrutura compartilhada

### `InMemoryDbFactory`

Cria um banco InMemory isolado por GUID por teste. Nunca compartilhar a mesma instância entre testes.

```csharp
public static ApplicationDbContext Create(string dbName = "")
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(string.IsNullOrEmpty(dbName) ? Guid.NewGuid().ToString() : dbName)
        .Options;
    return new ApplicationDbContext(options);
}
```

### `MockFactory`

Centraliza a criação de mocks repetidos:

```csharp
// Sempre retorna true para qualquer permissão
public static Mock<IPermissaoService> PermissaoSvcComAcesso();

// LogAsync é no-op
public static Mock<IAuditService> AuditSvcSilencioso();

// Retorna configurações padrão do PDV
public static Mock<IConfiguracaoService> ConfigSvcPadrao();
// Inclui: pdv_gerar_lancamento=1, control_estoque=1, pdv_mp_device_id="", app_name="Map-OS"
```

### `ClaimsPrincipalFactory`

```csharp
public static ClaimsPrincipal CriarUsuarioPdv(int usuarioId = 1);
// → claim sessao_tipo="pdv"

public static ClaimsPrincipal CriarUsuarioAdmin(int usuarioId = 1);

public static ClaimsPrincipal CriarUsuarioComDeveTrocarSenha(int usuarioId = 1);
// → adiciona claim deve_trocar_senha="true"
```

### `PageModelTestHelper`

Setup de `HttpContext`, `PageContext` e `TempData` via `DefaultHttpContext` para testar handlers de Razor Pages sem subir o pipeline HTTP completo.

### `EphemeralDataProtection`

```csharp
public static IFiscalCodigoProtector CriarProtector()
{
    var services = new ServiceCollection();
    services.AddDataProtection().UseEphemeralDataProtectionProvider();
    var sp = services.BuildServiceProvider();
    return new FiscalCodigoProtector(sp.GetRequiredService<IDataProtectionProvider>());
}
```

`EphemeralDataProtectionProvider` gera chaves em memória — sem disco, sem DPAPI, seguro para CI paralelo. **Nunca** usar `Mock<IDataProtector>` que devolve o input sem criptografar: passa os testes trivialmente mas não detecta bugs de purpose string.

---

## P0 — Segurança e integridade financeira

Bloqueador de release. Nenhum deploy sem estes testes passando.

---

### `LoginThrottleService` — 7 casos

| Caso | Cenário | Resultado esperado |
|------|---------|-------------------|
| 1 | Sem falhas | `EstaBloqueado` → `false` |
| 2 | 4 falhas consecutivas | Ainda não bloqueia |
| 3 | 5 falhas consecutivas | `EstaBloqueado` → `true` |
| 4 | 5 falhas → `TempoBloqueio` | Entre 14m55s e 15m00s |
| 5 | 4 falhas → `RegistrarSucesso` → 4 falhas | Não bloqueia (contador foi resetado) |
| 6 | 5 falhas → `RegistrarSucesso` | `EstaBloqueado` → `false` |
| 7 | 5 falhas no IP | Email não é bloqueado (chaves independentes) |

---

### `FiscalCodigoProtector` — 8 casos

> Usar `EphemeralDataProtectionProvider` — nunca `Mock<IDataProtector>`.

| Caso | Cenário | Resultado esperado |
|------|---------|-------------------|
| 1 | `Proteger("ABCD1234")` | Resultado ≠ `"ABCD1234"` |
| 2 | `Desproteger(Proteger(x))` | Igual ao original |
| 3 | `Verificar(codigo, Proteger(codigo))` | `true` |
| 4 | `Verificar("WRONG", Proteger("CERTO"))` | `false` |
| 5 | `Verificar("x", "lixo_nao_criptografado")` | `false` (testa o `catch`) |
| 6 | `EstaProtegido(Proteger("x"))` | `true` |
| 7 | `EstaProtegido("TEXTO_PLANO")` | `false` (detecção do fallback legado) |
| 8 | `Verificar("ABC", "ABC")` — comparação direta plaintext | `false` (o protector não aceita bypass) |

---

### `PermissaoService` — 10 casos

| Caso | Cenário | Resultado esperado |
|------|---------|-------------------|
| 1 | JSON `{"vPdv":1}` | `dict["vPdv"] == true` |
| 2 | JSON `{"vPdv":0}` | `dict["vPdv"] == false` |
| 3 | JSON `{"vPdv":"1"}` string | `true` |
| 4 | JSON `{"vPdv":true}` bool | `true` |
| 5 | JSON inválido | Dict vazio, sem exceção |
| 6 | `Permissoes == null` | Dict vazio |
| 7 | Segunda chamada com DB alterado | Retorna valor do cache (não o novo do DB) |
| 8 | `InvalidarCache` → nova chamada | Lê novo valor do banco |
| 9 | `TemPermissaoAsync` com atividade inexistente | `false` |
| 10 | `GetPermissaoIdByUsuarioIdAsync` segunda chamada | Usa cache |

---

### `PageModelBase.OnPageHandlerExecutionAsync` — 5 casos

| Caso | Claim `deve_trocar_senha` | Path | Resultado |
|------|--------------------------|------|-----------|
| 1 | Ausente | `/Produtos/Index` | `next()` chamado, sem redirect |
| 2 | `"true"` | `/Produtos/Index` | Redirect para `/MinhaConta/Index`, `next()` não chamado |
| 3 | `"true"` | `/MinhaConta/TrocarSenha` | `next()` chamado |
| 4 | `"true"` | `/Account/Logout` | `next()` chamado |
| 5 | `"true"` | `/minhaconta/perfil` (lowercase) | `next()` chamado — `StartsWith` é case-insensitive |

---

### `Pdv/Index.OnPostFinalizarVendaAsync` — 13 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | Sem sessão de caixa aberta | `{ok:false, erro:"Nenhuma sessão..."}` |
| 2 | Carrinho vazio (`Itens=[]`) | `{ok:false, erro:"Carrinho vazio."}` |
| 3 | Total zero após desconto | `{ok:false, erro:"Valor total inválido."}` |
| 4 | Desconto em `%`: subtotal=100, desconto=10% | `ValorTotal == 90.00` |
| 5 | Desconto em `R$`: subtotal=100, desconto=15 | `ValorTotal == 85.00` |
| 6 | `ClienteId=null`, sem "Consumidor Final" | Cria o cliente e vincula à venda |
| 7 | `ClienteId=null`, "Consumidor Final" já existe | Reutiliza sem duplicar |
| 8 | `FormaPagamento="dinheiro"`, total=50 | `SessaoCaixa.SaldoEsperado += 50` |
| 9 | `FormaPagamento="credito"`, total=50 | `SaldoEsperado` inalterado |
| 10 | `control_estoque="1"`, produto.Estoque=10, qty=3 | `produto.Estoque == 7` |
| 11 | `control_estoque="0"` | Estoque inalterado |
| 12 | `pdv_gerar_lancamento="1"` | `db.Lancamentos.Count() == 1` |
| 13 | `pdv_gerar_lancamento="0"` | `db.Lancamentos.Count() == 0` |

---

### `Pdv/Index` — Autorização Fiscal — 9 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | Código AES correto + PIN correto + dentro da validade | `{ok:true, fiscal:"Nome"}` |
| 2 | Código incorreto para todos os fiscais | `{ok:false, erro:"Cartão fiscal não reconhecido."}` |
| 3 | Código correto + PIN errado | `{ok:false, erro:"PIN incorreto."}` |
| 4 | `PinPrimeiroUso=true` | `{ok:false, pinExpirado:true, motivo:"primeiro_uso"}` |
| 5 | `PinAlteradoEm` há 16 dias | `{ok:false, pinExpirado:true, motivo:"expirado"}` |
| 6 | `PinAlteradoEm` há 10 dias, `PinPrimeiroUso=false` | `{ok:true}` |
| 7 | Código legado plaintext: `FiscalPdvCodigo == req.Codigo` | `{ok:true}` (fallback compatível) |
| 8 | `AlterarPinFiscal` com PIN < 4 dígitos | Erro de validação |
| 9 | `AlterarPinFiscal` bem-sucedido | `PinPrimeiroUso=false`, `PinAlteradoEm ≈ UtcNow` |

---

## P1 — Fluxos principais

### `Login.OnPostAsync` — 10 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | E-mail ou senha inválidos | Retorna página com `ErrorMessage` |
| 2 | Usuário inativo (`Situacao=false`) | `"E-mail ou senha inválidos."` |
| 3 | Conta expirada | `"Conta expirada."` |
| 4 | IP bloqueado pelo throttle | `"Muitas tentativas incorretas."` |
| 5 | Email bloqueado pelo throttle | Mesma mensagem |
| 6 | Login bem-sucedido | Throttle zerado (IP e email desbloqueados) |
| 7 | `Destino="pdv"` | Redirect para `/Pdv/AbrirCaixa` |
| 8 | `Destino="admin"` | Redirect para `/Index` |
| 9 | `PrimeiroAcesso=true` | Cookie emitido com claim `deve_trocar_senha="true"` |
| 10 | `PrimeiroAcesso=false` | Claim `deve_trocar_senha` ausente do cookie |

---

### `AbrirCaixa.OnPostAsync` — 4 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | Terminal com `Status="ocupado"` | `ModelState` inválido, erro em `PdvTerminalId` |
| 2 | Sessão já aberta para o operador | Redirect para `/Pdv/Index` |
| 3 | Abertura bem-sucedida | `terminal.Status == "ocupado"` |
| 4 | Abertura bem-sucedida, `SaldoInicial=250` | `sessao.SaldoEsperado == 250` |

---

### `FecharCaixa` — 3 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | 2 vendas dinheiro (R$50) + 1 PIX (R$100) + 1 crédito (R$200) | `TotalDinheiro=100`, `TotalPix=100`, `TotalCredito=200`, `TotalVendas=400` |
| 2 | `SaldoEsperado=200`, `SaldoInformado=195` | `sessao.Diferenca == -5` |
| 3 | Fechamento concluído | `terminal.Status == "disponivel"` |

---

### `ConfiguracaoService` — 5 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | Primeira chamada com 3 chaves no banco | Retorna dict com 3 entradas |
| 2 | Segunda chamada após alteração no banco | Retorna valor do cache (não o alterado) |
| 3 | `SetAsync` chave nova | Inserida no banco |
| 4 | `SetAsync` chave existente | Atualizada sem duplicar |
| 5 | `SetAsync` → `GetAllAsync` | Retorna novo valor (cache invalidado) |

---

## P2 — Lógica complementar

### `MercadoPagoService` — helpers puros (sem HTTP mock)

| Método | Casos |
|--------|-------|
| `MapStatus` | `"APPROVED"` → `"RECEIVED"` · `"CANCELLED"` → `"CANCELLED"` · `null` → `"PENDING"` |
| `ParseIsoDuration` | `"P3D"` → 3 dias · `"PT2H"` → 2h · `"P1DT12H"` → 1d12h · inválido → 3 dias (default) |
| `SplitName` | `"João Silva"` → `("João","Silva")` · `"Marcos"` → `("Marcos","")` |
| `SplitPhone` | `"11987654321"` → `("11","987654321")` · número curto → `("","numero")` |
| `GerarBoleto` | Tipo inválido → erro antes de chamar API · cliente sem rua → erro · cliente sem documento → erro · valor=0 → erro |

---

### `EscPosService` — 5 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | Bytes iniciais | `output[0]==0x1B`, `output[1]==0x40` (ESC @) |
| 2 | Bytes finais | Contêm `0x1D 0x56 0x01` (comando de corte) |
| 3 | `pdv_paper_cols="48"` | Separadores com 48 caracteres |
| 4 | `pdv_paper_cols="32"` | Separadores com 32 caracteres |
| 5 | Item com `Descricao="Produto Teste"` | Bytes contêm a string em encoding CP850 |

---

### `EmailQueueService` — 4 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | Fila vazia | SMTP não chamado |
| 2 | Email com `Status="failed"` | Após `RetryAsync`: `Status == "pending"` |
| 3 | Email com `Status="sending"` há > 5 min | Após `RetryAsync`: `Status == "pending"` |
| 4 | Email com `Status="sending"` há < 5 min | Permanece `"sending"` |

---

### `Pdv/Index` — Pausar/Retomar — 4 casos

| # | Cenário | Resultado esperado |
|---|---------|-------------------|
| 1 | `OnPostPausar` com sessão aberta | Cria `PdvPausa`, `terminal.Status == "pausado"` |
| 2 | `OnPostRetomar` com PIN errado (BCrypt.Verify falha) | `{ok:false, erro:"PIN incorreto."}` |
| 3 | `OnPostRetomar` com PIN correto | `pdvPausa.RetomadaEm ≈ UtcNow`, `terminal.Status == "ocupado"` |

---

## P3 — Testes de integração (PostgreSQL real)

### Por que cada teste precisa de banco real

| Teste | Motivo |
|-------|--------|
| `FinalizarVenda` transação completa | Índice único parcial `lancamentos.venda_id` não existe no InMemory |
| Duas vendas na mesma sessão | FK e sequences de identidade reais |
| Lancamento duplicado para mesma venda | PostgreSQL lança `DbUpdateException` — InMemory ignora |
| `AbrirCaixa` + índice `IX_sessoes_caixa_operador_status` | Não aplicado pelo InMemory |
| Ciclo abrir/fechar caixa | Transição de FK real entre tabelas |
| Login + cookie | `WebApplicationFactory` + pipeline HTTP completo |
| 5 falhas de login seguidas | Rate limiting por IP no middleware real |
| `PermissaoService` multi-escopo | Invalidação de cache entre escopos DI diferentes |
| `EmailQueueService.ProcessAsync` | Transição `pending → sending → sent` com SMTP mock |

---

### Casos de integração

```
FinalizarVenda_DoisProdutos_CriaVendaItensLancamento
  → 1 Venda, 2 ItemVenda, 1 Lancamento em uma única transação

FinalizarVenda_DuasVendasMesmoSessao_DoisLancamentosDistintos
  → VendaId único por Lancamento, sem violação de índice

FinalizarVenda_LancamentoDuplicadoMesmaVenda_LancaDbUpdateException
  → insert forçado de 2 Lancamentos com mesmo VendaId → exceção

AbrirCaixa_Sucesso_TerminalMarcadoOcupado
  → terminal seed "disponivel" → após handler: "ocupado"

AbrirFecharCaixa_CicloCompleto_TerminalVoltaParaDisponivel
  → abrir e fechar → terminal retorna para "disponivel"

Login_CredenciaisCorretas_EmiteCookie
  → POST /Account/Login via WebApplicationFactory → resposta tem Set-Cookie

Login_CincoFalhasMesmoIp_SextaRetornaBloqueio
  → 5 POSTs com senha errada → 6° retorna "Muitas tentativas incorretas."

PermissaoService_MultiEscopo_InvalidarCacheReleBanco
  → scope1 lê e cria cache → scope2 altera banco e invalida → scope3 lê valor novo

EmailQueue_ProcessAsync_MudaStatusParaSent
  → seed 1 email "pending" → ProcessAsync com SMTP mock → Status == "sent"
```

---

### Estratégia de isolamento entre testes

```
┌─────────────────────────────────────────────────────────────────┐
│  PostgresFixture (ICollectionFixture)                           │
│  → Um container por collection, compartilhado pelas classes     │
│  → Database.MigrateAsync() ao inicializar                       │
│                                                                 │
│  Por teste CRUD        →  BeginTransaction + Rollback           │
│  Por teste c/ SEQUENCE →  TRUNCATE em ordem de FK              │
│                           (itens_de_vendas, lancamentos,        │
│                            sessoes_caixa, vendas, logs,         │
│                            usuarios, permissoes, clientes)      │
└─────────────────────────────────────────────────────────────────┘
```

> **Nunca usar** `EnsureDeleted()` + `EnsureCreated()`. Isso perde os índices parciais da migration de produção.

### CI sem Docker disponível

Marcar todos os testes de integração com `[Trait("Category","Integration")]` e configurar o pipeline:

```yaml
# Testes unitários — sempre
- run: dotnet test tests/mapos-dotnet.Unit.Tests

# Testes de integração — apenas quando Docker disponível
- run: dotnet test tests/mapos-dotnet.Integration.Tests
  env:
    SKIP_INTEGRATION_TESTS: ${{ env.DOCKER_AVAILABLE == 'false' && 'true' || 'false' }}
```

---

## Sequência de implementação

| Sprint | Escopo | Estimativa |
|--------|--------|-----------|
| **1** | Helpers base (`InMemoryDbFactory`, `MockFactory`, `ClaimsPrincipalFactory`, `PageModelTestHelper`, `EphemeralDataProtection`) + `LoginThrottleService` + `FiscalCodigoProtector` + `PermissaoService` + `PageModelBase` | 2 dias |
| **2** | `PdvFinalizarVenda` + `PdvAutorizarFiscal` | 2 dias |
| **3** | `Login` + `AbrirCaixa` + `FecharCaixa` + `ConfiguracaoService` | 2 dias |
| **4** | `MercadoPago` helpers + `EscPosService` + `EmailQueue` + `PausarRetomar` | 2 dias |
| **5** | `PostgresFixture` + todos os testes de integração | 3 dias |

**Total estimado: ~11 dias úteis**

---

## Resumo de cobertura

| Categoria | Testes | Tipo |
|-----------|--------|------|
| `LoginThrottleService` | 7 | Unit |
| `FiscalCodigoProtector` | 8 | Unit |
| `PermissaoService` | 10 | Unit |
| `PageModelBase` | 5 | Unit |
| `Pdv/Index` — FinalizarVenda | 13 | Unit |
| `Pdv/Index` — AutorizarFiscal | 9 | Unit |
| `Login.OnPostAsync` | 10 | Unit |
| `AbrirCaixa` | 4 | Unit |
| `FecharCaixa` | 3 | Unit |
| `ConfiguracaoService` | 5 | Unit |
| `MercadoPagoService` helpers | ~12 | Unit |
| `EscPosService` | 5 | Unit |
| `EmailQueueService` | 4 | Unit |
| `Pdv` — PausarRetomar | 3 | Unit |
| Integração — PDV | 3 | Integration |
| Integração — Caixa | 2 | Integration |
| Integração — Auth | 2 | Integration |
| Integração — Permissões | 1 | Integration |
| Integração — Email | 1 | Integration |
| **Total** | **~111** | |
