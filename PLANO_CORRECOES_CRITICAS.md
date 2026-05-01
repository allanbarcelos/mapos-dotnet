# Plano de Implementação — Correções Críticas

> Gerado em 2026-05-01
> Referência: AVALIACAO.md

Este documento descreve as mudanças exatas necessárias para corrigir os 5 pontos críticos identificados. Cada item traz contexto do problema, arquivos afetados, código antes/depois e passos de verificação.

---

## Sumário

| # | Problema | Esforço estimado |
|---|----------|-----------------|
| C1 | Credenciais hardcoded no `appsettings.json` | 30 min |
| C2 | `FiscalPdvCodigo` em texto plano no banco | 2 h |
| C3 | Sem rate limiting no login (brute-force) | 1 h |
| C4 | Lançamento financeiro duplicado por venda | 45 min |
| C5 | `permissao_id` lido do cookie sem validação | 1,5 h |

**Total estimado: ~6 horas de desenvolvimento + testes.**

---

## C1 — Credenciais hardcoded no `appsettings.json`

### Problema
`appsettings.json` contém a senha do PostgreSQL em texto plano e está (ou pode estar) versionado no repositório. Qualquer pessoa com acesso ao repositório tem acesso ao banco.

```json
// appsettings.json — PROBLEMA
"DefaultConnection": "Host=localhost;Port=5432;Database=mapos;Username=postgres;Password=postgres"
```

### Solução
Substituir o valor real por um placeholder. As credenciais reais devem vir exclusivamente de:
- **Desenvolvimento local:** `appsettings.Development.json` (não versionado) ou User Secrets
- **Produção/Docker:** Docker secrets (já configurados no `docker-compose.yml`)

### Arquivos afetados
- `appsettings.json`
- `appsettings.Development.json` (criar se não existir)
- `.gitignore` (verificar que `appsettings.Development.json` está listado)

### Mudanças

**`appsettings.json`** — substituir connection string real por placeholder:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**`appsettings.Development.json`** — criar com credenciais locais de desenvolvimento:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mapos;Username=postgres;Password=postgres"
  }
}
```

**`.gitignore`** — confirmar presença da linha:
```
appsettings.Development.json
```

### Verificação
```bash
# Verificar que appsettings.json não tem senha
grep -i "password" appsettings.json   # deve retornar vazio ou só o placeholder

# Verificar que o app sobe normalmente em desenvolvimento
dotnet run
```

---

## C2 — `FiscalPdvCodigo` em texto plano no banco

### Problema
`FiscalPdvCodigo` é um identificador único gerado para o cartão do fiscal PDV. Está armazenado em **texto plano**. Em caso de vazamento do banco, todos os cartões fiscais ficam comprometidos.

O PIN fiscal (`FiscalPdvPin`) já está protegido com BCrypt, mas o código não.

### Análise de design
`FiscalPdvCodigo` precisa ser **exibido** na impressão do cartão (`Pages/Usuarios/CartaoFiscal.cshtml`) — portanto BCrypt (unidirecional) não é aplicável diretamente. A solução correta é **criptografia simétrica (AES)** usando o `IDataProtectionProvider` do próprio ASP.NET Core.

- **Geração:** código gerado → criptografado → salvo no banco
- **Exibição:** criptografado no banco → descriptografado → impresso no cartão
- **Verificação no PDV:** código escaneado → criptografado → comparado com valor no banco (comparação de ciphertext, não plaintext)

> O `IDataProtectionProvider` usa chaves gerenciadas pelo ASP.NET Core (rotacionadas automaticamente, armazenadas fora do banco de dados).

### Arquivos afetados
- `Services/FiscalCodigoProtector.cs` ← **novo**
- `Program.cs`
- `Pages/Usuarios/Adicionar.cshtml.cs`
- `Pages/Usuarios/Editar.cshtml.cs`
- `Pages/Usuarios/CartaoFiscal.cshtml.cs`
- `Pages/Pdv/Index.cshtml.cs` (onde o código é verificado durante autorização fiscal)
- Migration nova

### Mudanças

**`Services/FiscalCodigoProtector.cs`** — novo serviço de proteção:
```csharp
using Microsoft.AspNetCore.DataProtection;

namespace mapos_dotnet.Services;

public interface IFiscalCodigoProtector
{
    string Proteger(string codigo);
    string Desproteger(string codigoProtegido);
    bool Verificar(string codigoInformado, string codigoProtegido);
}

public class FiscalCodigoProtector : IFiscalCodigoProtector
{
    private readonly IDataProtector _protector;

    public FiscalCodigoProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FiscalPdvCodigo.v1");
    }

    public string Proteger(string codigo)
        => _protector.Protect(codigo);

    public string Desproteger(string codigoProtegido)
        => _protector.Unprotect(codigoProtegido);

    public bool Verificar(string codigoInformado, string codigoProtegido)
    {
        try { return Desproteger(codigoProtegido) == codigoInformado; }
        catch { return false; }
    }
}
```

**`Program.cs`** — registrar o serviço (adicionar após `AddMemoryCache`):
```csharp
builder.Services.AddDataProtection();
builder.Services.AddScoped<IFiscalCodigoProtector, FiscalCodigoProtector>();
```

**`Pages/Usuarios/Adicionar.cshtml.cs`** — injetar e usar o protector:
```csharp
// Adicionar IFiscalCodigoProtector ao construtor:
public AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IFiscalCodigoProtector fiscalProtector)   // <-- novo
    : base(db, permissaoSvc, auditSvc, configSvc)
{
    _fiscalProtector = fiscalProtector;
}

private readonly IFiscalCodigoProtector _fiscalProtector;

// Em OnPostAsync, substituir a linha de geração do código:
// ANTES:
usuario.FiscalPdvCodigo = $"FISCAL{Guid.NewGuid():N}"[..20].ToUpperInvariant();

// DEPOIS:
var codigoGerado = $"FISCAL{Guid.NewGuid():N}"[..20].ToUpperInvariant();
usuario.FiscalPdvCodigo = _fiscalProtector.Proteger(codigoGerado);
```

**`Pages/Usuarios/Editar.cshtml.cs`** — mesma injeção + geração de código:
```csharp
// Construtor: adicionar IFiscalCodigoProtector fiscalProtector
// Em OnPostAsync, mesma substituição:
var codigoGerado = $"FISCAL{Guid.NewGuid():N}"[..20].ToUpperInvariant();
usuario.FiscalPdvCodigo = _fiscalProtector.Proteger(codigoGerado);
```

**`Pages/Usuarios/CartaoFiscal.cshtml.cs`** — descriptografar para exibição:
```csharp
// Adicionar IFiscalCodigoProtector ao construtor
// Em OnGetAsync, após carregar o usuário:
// ANTES:
Usuario = usuario;

// DEPOIS:
Usuario = usuario;
// Descriptografar para exibição no cartão
try
{
    CodigoParaExibir = _fiscalProtector.Desproteger(usuario.FiscalPdvCodigo!);
}
catch
{
    // Código gerado antes da criptografia (migração pendente) — exibir aviso
    CodigoParaExibir = usuario.FiscalPdvCodigo ?? "";
    CodigoLegado = true;
}

// Adicionar propriedades:
public string CodigoParaExibir { get; set; } = "";
public bool CodigoLegado { get; set; }
```

**`Pages/Usuarios/CartaoFiscal.cshtml`** — usar `CodigoParaExibir` no lugar de `Usuario.FiscalPdvCodigo`:
```html
<!-- ANTES -->
<span>@Model.Usuario.FiscalPdvCodigo</span>

<!-- DEPOIS -->
@if (Model.CodigoLegado)
{
    <div class="text-amber-600 text-xs mb-2">
        ⚠️ Cartão gerado antes da proteção. Regenere para atualizar a segurança.
    </div>
}
<span>@Model.CodigoParaExibir</span>
```

**`Pages/Pdv/Index.cshtml.cs`** — verificação no PDV:
```csharp
// Adicionar IFiscalCodigoProtector ao construtor do PdvIndexModel

// Na lógica de autorização fiscal, substituir comparação direta:
// ANTES:
var fiscal = await Db.Usuarios.FirstOrDefaultAsync(u =>
    u.FiscalPdv && u.FiscalPdvCodigo == codigoEscaneado);

// DEPOIS:
var candidatos = await Db.Usuarios
    .Where(u => u.FiscalPdv && u.FiscalPdvCodigo != null)
    .ToListAsync();
var fiscal = candidatos.FirstOrDefault(u =>
    _fiscalProtector.Verificar(codigoEscaneado, u.FiscalPdvCodigo!));
```

### Migration
Criar migration para re-criptografar registros existentes:
```csharp
// Migrations/YYYYMMDDHHMMSS_EncryptFiscalPdvCodigo.cs
// A migration em si não altera schema (coluna continua string)
// A re-criptografia deve ser feita via script de data migration
// executado UMA VEZ após o deploy da nova versão:

// Script separado (executar manualmente ou via hosted service one-shot):
var usuarios = db.Usuarios.Where(u => u.FiscalPdvCodigo != null).ToList();
foreach (var u in usuarios)
{
    // Se não foi ainda protegido (não começa com o prefixo do IDataProtector)
    try { protector.Desproteger(u.FiscalPdvCodigo!); }  // já protegido
    catch
    {
        u.FiscalPdvCodigo = protector.Proteger(u.FiscalPdvCodigo!);
    }
}
db.SaveChanges();
```

### Verificação
```bash
# Cadastrar usuário fiscal, imprimir cartão — código deve aparecer normalmente
# Simular login PDV com código escaneado — autorização deve funcionar
# Acessar DB diretamente: SELECT fiscal_pdv_codigo FROM usuarios WHERE fiscal_pdv = true
# Valor deve ser uma string opaca (ciphertext), não "FISCAL..."
```

---

## C3 — Sem rate limiting no login

### Problema
`Pages/Account/Login.cshtml.cs` processa tentativas de login sem nenhum controle de frequência. Um atacante pode tentar milhares de senhas sem qualquer bloqueio.

### Solução
Implementar bloqueio progressivo por IP + email usando `IMemoryCache`:
- Após **5 tentativas falhas** no mesmo e-mail ou IP: bloqueio de **15 minutos**
- Ao fazer login com sucesso: resetar contador

> Optamos por `IMemoryCache` (já no projeto) em vez do middleware de rate limiting (`AddRateLimiter`) para ter controle granular por e-mail, não apenas por IP.

### Arquivos afetados
- `Services/LoginThrottleService.cs` ← **novo**
- `Program.cs`
- `Pages/Account/Login.cshtml.cs`

### Mudanças

**`Services/LoginThrottleService.cs`** — novo serviço:
```csharp
using Microsoft.Extensions.Caching.Memory;

namespace mapos_dotnet.Services;

public interface ILoginThrottleService
{
    bool EstaBloqueado(string chave);
    void RegistrarFalha(string chave);
    void RegistrarSucesso(string chave);
    TimeSpan? TempoBloqueio(string chave);
}

public class LoginThrottleService(IMemoryCache cache) : ILoginThrottleService
{
    private const int MaxTentativas  = 5;
    private const int BloqueioMinutos = 15;

    // Chave pode ser e-mail ou IP
    private string ChaveFalhas(string chave)  => $"login_falhas:{chave}";
    private string ChaveBloqueio(string chave) => $"login_bloqueio:{chave}";

    public bool EstaBloqueado(string chave)
        => cache.TryGetValue(ChaveBloqueio(chave), out _);

    public TimeSpan? TempoBloqueio(string chave)
    {
        if (cache.TryGetValue<DateTimeOffset>(ChaveBloqueio(chave), out var expira))
            return expira - DateTimeOffset.UtcNow;
        return null;
    }

    public void RegistrarFalha(string chave)
    {
        var keyFalhas = ChaveFalhas(chave);
        var falhas = cache.GetOrCreate(keyFalhas, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BloqueioMinutos);
            return 0;
        });

        falhas++;
        cache.Set(keyFalhas, falhas,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BloqueioMinutos)
            });

        if (falhas >= MaxTentativas)
        {
            var expira = DateTimeOffset.UtcNow.AddMinutes(BloqueioMinutos);
            cache.Set(ChaveBloqueio(chave), expira,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expira
                });
        }
    }

    public void RegistrarSucesso(string chave)
    {
        cache.Remove(ChaveFalhas(chave));
        cache.Remove(ChaveBloqueio(chave));
    }
}
```

**`Program.cs`** — registrar (adicionar após `AddMemoryCache`):
```csharp
builder.Services.AddSingleton<ILoginThrottleService, LoginThrottleService>();
```

> Registrar como `Singleton` para que o estado sobreviva entre requests.

**`Pages/Account/Login.cshtml.cs`** — aplicar throttle:
```csharp
// Adicionar ILoginThrottleService ao construtor:
public LoginModel(
    ApplicationDbContext db,
    IAuditService auditSvc,
    ILoginThrottleService throttle)   // <-- novo
{
    _db      = db;
    _auditSvc = auditSvc;
    _throttle = throttle;
}

private readonly ApplicationDbContext _db;
private readonly IAuditService _auditSvc;
private readonly ILoginThrottleService _throttle;

public async Task<IActionResult> OnPostAsync()
{
    if (!ModelState.IsValid)
        return Page();

    var ip    = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var email = Input.Email.Trim().ToLowerInvariant();

    // Verificar bloqueio por IP e por e-mail
    if (_throttle.EstaBloqueado(ip) || _throttle.EstaBloqueado(email))
    {
        var espera = _throttle.TempoBloqueio(ip) ?? _throttle.TempoBloqueio(email);
        var minutos = (int)Math.Ceiling(espera?.TotalMinutes ?? BloqueioMinutos);
        ErrorMessage = $"Muitas tentativas incorretas. Tente novamente em {minutos} minuto(s).";
        return Page();
    }

    var usuario = await _db.Usuarios
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == Input.Email && u.Situacao);

    if (usuario is null || !BCrypt.Net.BCrypt.Verify(Input.Senha, usuario.Senha))
    {
        // Registrar falha nos dois eixos (IP e e-mail)
        _throttle.RegistrarFalha(ip);
        _throttle.RegistrarFalha(email);
        ErrorMessage = "E-mail ou senha inválidos.";
        return Page();
    }

    if (usuario.DataExpiracao.HasValue && usuario.DataExpiracao.Value < DateOnly.FromDateTime(DateTime.Today))
    {
        ErrorMessage = "Conta expirada. Entre em contato com o administrador.";
        return Page();
    }

    // Login bem-sucedido — limpar contadores
    _throttle.RegistrarSucesso(ip);
    _throttle.RegistrarSucesso(email);

    // ... resto do código de login permanece igual
}
```

**`Pages/Account/Login.cshtml`** — exibir mensagem de bloqueio (já usa `ErrorMessage`, funciona automaticamente).

### Verificação
```bash
# Testar manualmente: tentar login com senha errada 5 vezes
# Na 6ª tentativa, deve aparecer a mensagem de bloqueio
# Aguardar 15 minutos (ou reduzir temporariamente para teste) e tentar novamente
# Login com credenciais corretas após reset deve funcionar
```

---

## C4 — Lançamento financeiro duplicado por venda

### Problema
Quando o PDV fecha uma venda, cria automaticamente um `Lancamento` vinculado pela `VendaId`. Não existe constraint de unicidade na tabela — um segundo lançamento pode ser criado manualmente para a mesma venda, duplicando a receita sem nenhum aviso.

### Solução
1. Adicionar índice único parcial em `lancamentos.venda_id` (`WHERE venda_id IS NOT NULL`) no banco
2. Configurar o índice no `ApplicationDbContext`

> Índice **parcial** (filtrado) porque `venda_id` é nullable — múltiplos lançamentos sem venda associada são legítimos.

### Arquivos afetados
- `Data/ApplicationDbContext.cs`
- Nova migration

### Mudanças

**`Data/ApplicationDbContext.cs`** — adicionar índice único na configuração de `Lancamento`:

Localizar o bloco `modelBuilder.Entity<Lancamento>` e adicionar:
```csharp
// ADICIONAR dentro do bloco de configuração de Lancamento:
entity.HasIndex(l => l.VendaId)
      .IsUnique()
      .HasFilter("venda_id IS NOT NULL");
```

### Migration
```bash
dotnet ef migrations add AddUniqueVendaIdInLancamentos
```

O arquivo gerado deve conter:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateIndex(
        name: "IX_lancamentos_venda_id",
        table: "lancamentos",
        column: "venda_id",
        unique: true,
        filter: "venda_id IS NOT NULL");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropIndex(
        name: "IX_lancamentos_venda_id",
        table: "lancamentos");
}
```

> **Atenção:** antes de rodar a migration em produção, verificar se já existem registros duplicados:
> ```sql
> SELECT venda_id, COUNT(*) FROM lancamentos
> WHERE venda_id IS NOT NULL
> GROUP BY venda_id HAVING COUNT(*) > 1;
> ```
> Se existirem duplicatas, resolver manualmente antes de aplicar o índice.

### Verificação
```bash
# Tentar criar dois lançamentos para a mesma venda via código ou direto no banco
# O banco deve rejeitar com unique constraint violation
# Vendas novas criadas pelo PDV devem continuar funcionando normalmente
```

---

## C5 — `permissao_id` no cookie sem validação

### Problema
O cookie de sessão armazena `permissao_id` como claim (`Login.cshtml.cs:63`). `PageModelBase` lê este valor diretamente do cookie (`PageModelBase.cs:35-36`) para decidir as permissões do usuário. Um usuário mal-intencionado pode editar o cookie (via devtools ou proxy) para declarar um `permissao_id` diferente — como o perfil de Administrador.

```csharp
// PageModelBase.cs:35-36 — PROBLEMA: confia no cookie
protected int PermissaoId =>
    int.TryParse(User.FindFirstValue("permissao_id"), out var p) ? p : 0;
```

### Solução
Resolver `PermissaoId` sempre a partir do banco de dados (usando `UsuarioId` que vem de `ClaimTypes.NameIdentifier` — este claim é assinado pela infraestrutura de autenticação do ASP.NET Core e **não pode ser forjado sem a chave do servidor**).

Para evitar uma query extra por request, adicionar cache por `usuarioId` com TTL curto (5 minutos), invalidado quando o usuário é editado.

### Arquivos afetados
- `Services/PermissaoService.cs`
- `Pages/PageModelBase.cs`
- `Pages/Account/Login.cshtml.cs` (remover claim desnecessário)
- `Pages/Usuarios/Editar.cshtml.cs` (invalidar cache ao editar usuário)

### Mudanças

**`Services/PermissaoService.cs`** — adicionar lookup por `usuarioId`:
```csharp
// Adicionar ao interface IPermissaoService:
Task<int> GetPermissaoIdByUsuarioIdAsync(int usuarioId);
void InvalidarCachePorUsuario(int usuarioId);

// Adicionar implementação em PermissaoService:
public async Task<int> GetPermissaoIdByUsuarioIdAsync(int usuarioId)
{
    var cacheKey = $"usuario_permissao_id:{usuarioId}";
    if (cache.TryGetValue(cacheKey, out int permissaoId))
        return permissaoId;

    permissaoId = await db.Usuarios
        .AsNoTracking()
        .Where(u => u.Id == usuarioId)
        .Select(u => u.PermissaoId)
        .FirstOrDefaultAsync();

    cache.Set(cacheKey, permissaoId, TimeSpan.FromMinutes(5));
    return permissaoId;
}

public void InvalidarCachePorUsuario(int usuarioId)
{
    cache.Remove($"usuario_permissao_id:{usuarioId}");
    // Nota: o cache de permissoes (permissao_{id}) é invalidado separadamente
    // quando o perfil de permissão for editado
}
```

**`Pages/PageModelBase.cs`** — resolver `PermissaoId` do banco:
```csharp
// REMOVER propriedade síncrona que lê do cookie:
// protected int PermissaoId =>
//     int.TryParse(User.FindFirstValue("permissao_id"), out var p) ? p : 0;

// SUBSTITUIR por campo lazy-loaded assíncrono:
private int? _permissaoId;

protected async Task<int> GetPermissaoIdAsync()
{
    if (_permissaoId.HasValue)
        return _permissaoId.Value;

    _permissaoId = await PermissaoSvc.GetPermissaoIdByUsuarioIdAsync(UsuarioId);
    return _permissaoId.Value;
}

// ATUALIZAR TemPermissaoAsync para usar o novo método:
protected async Task<bool> TemPermissaoAsync(string atividade)
{
    var permId = await GetPermissaoIdAsync();
    return await PermissaoSvc.TemPermissaoAsync(permId, atividade);
}
```

**`Pages/Account/Login.cshtml.cs`** — remover claim `permissao_id` do cookie (não é mais necessário):
```csharp
// ANTES:
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
    new(ClaimTypes.Name,           usuario.Nome),
    new(ClaimTypes.Email,          usuario.Email),
    new("permissao_id",            usuario.PermissaoId.ToString()),  // <-- REMOVER
    new("url_image",               usuario.UrlImageUser ?? string.Empty),
    new("sessao_tipo",             sessaoTipo),
};

// DEPOIS:
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
    new(ClaimTypes.Name,           usuario.Nome),
    new(ClaimTypes.Email,          usuario.Email),
    new("url_image",               usuario.UrlImageUser ?? string.Empty),
    new("sessao_tipo",             sessaoTipo),
};
```

**`Pages/Usuarios/Editar.cshtml.cs`** — invalidar cache ao editar usuário:
```csharp
// Em OnPostAsync, após SaveChangesAsync():
await Db.SaveChangesAsync();
PermissaoSvc.InvalidarCachePorUsuario(usuario.Id);  // <-- ADICIONAR
await AuditarAsync($"Editou usuário #{usuario.Id} - {usuario.Nome}");
```

> **Nota:** o `PermissaoSvc` já está disponível em `PageModelBase` via `protected readonly IPermissaoService PermissaoSvc`.

### Impacto em performance
A nova implementação adiciona **1 query ao banco de dados por sessão** (a primeira chamada a `TemPermissaoAsync` em cada request carrega `PermissaoId` do banco; as seguintes usam `_permissaoId` em memória no request atual). O resultado é cacheado por 5 minutos no `IMemoryCache`, então na prática:
- **Primeiro request após login ou expiração do cache:** 2 queries (PermissaoId + Permissoes)
- **Demais requests:** 0 queries adicionais (servidos pelo cache)

### Verificação
```bash
# 1. Fazer login normal — sistema deve funcionar igual
# 2. Inspecionar cookie de sessão no browser — não deve mais existir permissao_id
# 3. Editar manualmente o cookie (via devtools) — não deve alterar permissões
# 4. Editar permissão de um usuário — mudança deve refletir em até 5 minutos
#    (ou imediatamente se o usuário fizer logout e login novamente)
```

---

## Ordem de implementação recomendada

```
1. C1 — appsettings.json        (mais simples, risco de regressão zero)
2. C4 — índice venda_id         (migration simples, risco de regressão baixo)
3. C3 — rate limiting login     (serviço novo isolado, sem tocar no fluxo existente)
4. C5 — permissao_id do cookie  (maior impacto arquitetural, testar bem)
5. C2 — FiscalPdvCodigo         (maior complexidade, inclui data migration)
```

## Checklist de testes após implementação

```
[ ] C1: dotnet run em desenvolvimento sem appsettings.json modificado — deve falhar
[ ] C1: dotnet run com appsettings.Development.json correto — deve subir normalmente
[ ] C2: Cartão fiscal imprime corretamente após re-criptografia
[ ] C2: Autorização no PDV via código escaneado continua funcionando
[ ] C2: Banco não mostra código fiscal em plaintext
[ ] C3: 5 tentativas de login erradas bloqueiam por 15 min
[ ] C3: Login correto imediatamente após bloqueio falha até expirar
[ ] C3: Login correto funciona normalmente em condições normais
[ ] C4: Criar lançamento manual para venda já lançada → deve ser rejeitado
[ ] C4: PDV fechando venda com lançamento automático → deve funcionar
[ ] C5: Cookie não contém mais permissao_id
[ ] C5: Alterar permissão de usuário logado → reflete em até 5 min
[ ] C5: Editar cookie manualmente → não altera permissões do usuário
[ ] C5: Login, navegação em todas as seções principais → sem regressão
```
