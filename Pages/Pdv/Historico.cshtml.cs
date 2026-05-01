using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Pdv;

public class HistoricoModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<SessaoCaixa> Sessoes { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vPdv")) return SemPermissao();

        Sessoes = await Db.SessoesCaixa
            .AsNoTracking()
            .Include(s => s.Operador)
            .Include(s => s.Vendas)
            .Where(s => s.OperadorId == UsuarioId)
            .OrderByDescending(s => s.AbertoEm)
            .Take(50)
            .ToListAsync();

        await SetLayoutDataAsync();
        return Page();
    }
}
