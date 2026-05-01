using System.Security.Claims;
using mapos_dotnet.Data;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages;

[Authorize]
public abstract class PageModelBase : PageModel
{
    protected readonly ApplicationDbContext Db;
    protected readonly IPermissaoService PermissaoSvc;
    protected readonly IAuditService AuditSvc;
    protected readonly IConfiguracaoService ConfigSvc;

    protected PageModelBase(
        ApplicationDbContext db,
        IPermissaoService permissaoSvc,
        IAuditService auditSvc,
        IConfiguracaoService configSvc)
    {
        Db           = db;
        PermissaoSvc = permissaoSvc;
        AuditSvc     = auditSvc;
        ConfigSvc    = configSvc;
    }

    protected int UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected string UsuarioNome => User.Identity?.Name ?? string.Empty;

    /// <summary>true quando o usuário fez login explicitamente na sessão PDV.</summary>
    protected bool SessaoPdv =>
        User.FindFirstValue("sessao_tipo") == "pdv";

    /// <summary>
    /// Retorna redirecionamento para logout quando a sessão não é PDV.
    /// Use nas páginas operacionais do PDV (AbrirCaixa, Index, FecharCaixa).
    /// </summary>
    protected IActionResult SomenteSessionPdv()
    {
        TempData["Error"] = "Acesso negado. Para usar o PDV selecione a opção PDV na tela de login.";
        return RedirectToPage("/Account/Logout");
    }

    protected string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    // Cache por request: evita múltiplas queries ao banco na mesma requisição
    private int? _permissaoIdCache;

    /// <summary>
    /// Retorna o PermissaoId real do usuário consultando o banco de dados
    /// (via cache de 5 min no IMemoryCache + cache por request).
    /// Nunca lê do cookie — o UsuarioId vem de ClaimTypes.NameIdentifier,
    /// que é assinado pelo ASP.NET Core e não pode ser forjado.
    /// </summary>
    private async Task<int> GetPermissaoIdAsync()
    {
        if (_permissaoIdCache.HasValue)
            return _permissaoIdCache.Value;

        _permissaoIdCache = await PermissaoSvc.GetPermissaoIdByUsuarioIdAsync(UsuarioId);
        return _permissaoIdCache.Value;
    }

    protected async Task<bool> TemPermissaoAsync(string atividade)
    {
        var permId = await GetPermissaoIdAsync();
        return await PermissaoSvc.TemPermissaoAsync(permId, atividade);
    }

    protected IActionResult SemPermissao()
    {
        TempData["Error"] = "Você não tem permissão para acessar este recurso.";
        return RedirectToPage("/Index");
    }

    protected async Task AuditarAsync(string tarefa)
        => await AuditSvc.LogAsync(UsuarioNome, tarefa, Ip, UsuarioId);

    // ── Política de senha — intercepta TODOS os requests ─────────────────────
    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        // Redireciona para troca obrigatória se o claim estiver presente,
        // exceto quando o próprio usuário já está em MinhaConta ou Account/Logout.
        var deveTrocar = User.FindFirstValue("deve_trocar_senha") == "true";
        if (deveTrocar)
        {
            var path = HttpContext.Request.Path.Value ?? "";
            var isExempt = path.StartsWith("/MinhaConta", StringComparison.OrdinalIgnoreCase)
                        || path.StartsWith("/Account",   StringComparison.OrdinalIgnoreCase);
            if (!isExempt)
            {
                context.Result = RedirectToPage("/MinhaConta/Index");
                return;
            }
        }

        await next();
    }

    protected async Task SetLayoutDataAsync()
    {
        var configs = await ConfigSvc.GetAllAsync();
        ViewData["AppName"]  = configs.GetValueOrDefault("app_name", "Map-OS");
        ViewData["AppTheme"] = configs.GetValueOrDefault("app_theme", "white");
        var hora = DateTime.Now.Hour;
        ViewData["Saudacao"] = hora < 12 ? "Bom dia," : hora < 18 ? "Boa tarde," : "Boa noite,";

        // Alerta de senha antiga (>45 dias) — não bloqueia, apenas avisa
        if (UsuarioId > 0 && User.FindFirstValue("deve_trocar_senha") != "true")
        {
            var usuario = await Db.Usuarios.AsNoTracking()
                .Where(u => u.Id == UsuarioId)
                .Select(u => new { u.SenhaAlteradaEm })
                .FirstOrDefaultAsync();

            if (usuario is not null)
            {
                var idadeSenha = usuario.SenhaAlteradaEm.HasValue
                    ? (DateTime.UtcNow - usuario.SenhaAlteradaEm.Value).TotalDays
                    : double.MaxValue;

                if (idadeSenha > 45)
                    ViewData["AlerteSenhaIdade"] = (int)idadeSenha;
            }
        }
    }
}
