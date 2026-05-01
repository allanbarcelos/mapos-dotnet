using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Permissoes;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<Permissao> Items { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cPermissao")) return SemPermissao();
        await SetLayoutDataAsync();
        Items = await Db.Permissoes.OrderBy(p => p.Nome).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        if (!await TemPermissaoAsync("cPermissao")) return SemPermissao();
        var p = await Db.Permissoes.FindAsync(id);
        if (p is not null) { p.Situacao = !p.Situacao; await Db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
