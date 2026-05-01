using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Clientes;

public class VisualizarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Cliente Cliente { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vCliente")) return SemPermissao();
        await SetLayoutDataAsync();

        var c = await Db.Clientes
            .AsNoTracking()
            .Include(x => x.OsList.OrderByDescending(o => o.Id)).ThenInclude(o => o.Usuario)
            .Include(x => x.Vendas.OrderByDescending(v => v.Id))
            .Include(x => x.Equipamentos).ThenInclude(e => e.Marca)
            .Include(x => x.Cobrancas.OrderByDescending(c => c.Id))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c is null) return NotFound();
        Cliente = c;
        return Page();
    }
}
