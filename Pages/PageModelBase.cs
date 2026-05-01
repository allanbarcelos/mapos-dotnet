using System.Security.Claims;
using mapos_dotnet.Data;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

    protected async Task SetLayoutDataAsync()
    {
        var configs = await ConfigSvc.GetAllAsync();
        ViewData["AppName"]  = configs.GetValueOrDefault("app_name", "Map-OS");
        ViewData["AppTheme"] = configs.GetValueOrDefault("app_theme", "white");
        var hora = DateTime.Now.Hour;
        ViewData["Saudacao"] = hora < 12 ? "Bom dia," : hora < 18 ? "Boa tarde," : "Boa noite,";
    }
}
