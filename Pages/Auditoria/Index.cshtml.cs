using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Auditoria;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<Log> Items  { get; set; } = [];
    public int CurrentPage  { get; set; } = 1;
    public int TotalPages   { get; set; } = 1;
    public string? Q        { get; set; }

    public async Task<IActionResult> OnGetAsync(string? q, int page = 1)
    {
        if (!await TemPermissaoAsync("cAuditoria")) return SemPermissao();
        await SetLayoutDataAsync();
        Q = q; CurrentPage = page;
        var query = Db.Logs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(q))
            query = query.Where(l => l.Usuario.Contains(q) || l.Tarefa.Contains(q));
        var total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / 20.0);
        Items = await query.OrderByDescending(l => l.Id).Skip((page - 1) * 20).Take(20).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostLimparAsync()
    {
        if (!await TemPermissaoAsync("cAuditoria")) return SemPermissao();
        Db.Logs.RemoveRange(Db.Logs);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Logs apagados.";
        return RedirectToPage();
    }
}
