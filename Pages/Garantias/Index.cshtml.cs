using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Garantias;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IList<Garantia> Garantias { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vGarantia"))
            return SemPermissao();

        await SetLayoutDataAsync();
        PodeAdicionar = await TemPermissaoAsync("aGarantia");
        Garantias = await Db.Garantias.AsNoTracking().OrderByDescending(g => g.Id).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dGarantia"))
            return SemPermissao();

        var garantia = await Db.Garantias.FindAsync(id);
        if (garantia is not null)
        {
            Db.Garantias.Remove(garantia);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Excluiu garantia #{id} - {garantia.RefGarantia}");
            TempData["Success"] = "Garantia excluída com sucesso.";
        }

        return RedirectToPage();
    }
}
