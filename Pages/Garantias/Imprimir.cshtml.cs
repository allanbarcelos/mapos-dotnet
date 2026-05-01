using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Garantias;

public class ImprimirModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Garantia Garantia { get; set; } = null!;
    public Models.Emitente? Emitente { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vGarantia")) return SemPermissao();

        var garantia = await Db.Garantias
            .AsNoTracking()
            .Include(g => g.Usuario)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (garantia is null) return NotFound();
        Garantia = garantia;
        Emitente = await Db.Emitente.AsNoTracking().FirstOrDefaultAsync();

        var configs = await ConfigSvc.GetAllAsync();
        ViewData["AppName"] = configs.GetValueOrDefault("app_name", "Map-OS");
        return Page();
    }
}
