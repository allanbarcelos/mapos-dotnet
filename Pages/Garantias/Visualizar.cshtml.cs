using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Garantias;

public class VisualizarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Garantia Garantia { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vGarantia"))
            return SemPermissao();

        var garantia = await Db.Garantias.FindAsync(Id);
        if (garantia is null)
        {
            TempData["Error"] = "Garantia não encontrada.";
            return RedirectToPage("Index");
        }

        Garantia = garantia;
        await SetLayoutDataAsync();
        return Page();
    }
}
