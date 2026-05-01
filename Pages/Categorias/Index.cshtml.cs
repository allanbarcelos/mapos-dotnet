using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Categorias;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IList<Categoria> Categorias { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vCategoria"))
            return SemPermissao();

        await SetLayoutDataAsync();
        PodeAdicionar = await TemPermissaoAsync("aCategoria");
        Categorias = await Db.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dCategoria"))
            return SemPermissao();

        var categoria = await Db.Categorias.FindAsync(id);
        if (categoria is not null)
        {
            Db.Categorias.Remove(categoria);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Excluiu categoria #{id} - {categoria.Nome}");
            TempData["Success"] = "Categoria excluída com sucesso.";
        }

        return RedirectToPage();
    }
}
