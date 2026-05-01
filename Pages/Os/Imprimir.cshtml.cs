using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Os;

public class ImprimirModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Models.Os Os { get; set; } = null!;
    public Models.Emitente? Emitente { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vOs")) return SemPermissao();

        var os = await Db.Os
            .AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .Include(o => o.ProdutosOs)
            .Include(o => o.ServicosOs)
            .Include(o => o.EquipamentosOs).ThenInclude(e => e.Equipamento)
            .Include(o => o.Anotacoes)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (os is null) return NotFound();
        Os = os;
        Emitente = await Db.Emitente.AsNoTracking().FirstOrDefaultAsync();

        var configs = await ConfigSvc.GetAllAsync();
        ViewData["AppName"] = configs.GetValueOrDefault("app_name", "Map-OS");
        return Page();
    }
}
