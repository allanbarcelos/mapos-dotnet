using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Vendas;

public class ImprimirModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Venda Venda { get; set; } = null!;
    public Models.Emitente? Emitente { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vVenda")) return SemPermissao();

        var venda = await Db.Vendas
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venda is null) return NotFound();
        Venda = venda;
        Emitente = await Db.Emitente.AsNoTracking().FirstOrDefaultAsync();

        var configs = await ConfigSvc.GetAllAsync();
        ViewData["AppName"] = configs.GetValueOrDefault("app_name", "Map-OS");
        return Page();
    }
}
