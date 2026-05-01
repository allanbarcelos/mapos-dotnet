using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Marcas;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IList<Marca> Marcas { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vMarca"))
            return SemPermissao();

        await SetLayoutDataAsync();
        PodeAdicionar = await TemPermissaoAsync("aMarca");
        Marcas = await Db.Marcas.AsNoTracking().OrderBy(m => m.Nome).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dMarca"))
            return SemPermissao();

        var marca = await Db.Marcas.FindAsync(id);
        if (marca is not null)
        {
            Db.Marcas.Remove(marca);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Excluiu marca #{id} - {marca.Nome}");
            TempData["Success"] = "Marca excluída com sucesso.";
        }

        return RedirectToPage();
    }
}
