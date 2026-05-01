using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Pdv.Terminais;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IReadOnlyList<PdvTerminal> Terminais { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        await SetLayoutDataAsync();
        Terminais = await Db.PdvTerminais
            .Include(t => t.SessaoCaixa).ThenInclude(s => s!.Operador)
            .OrderBy(t => t.Nome)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        var terminal = await Db.PdvTerminais.FindAsync(id);
        if (terminal is null) return NotFound();
        if (terminal.Status != "disponivel")
        {
            TempData["Error"] = "Não é possível excluir um terminal em uso.";
            return RedirectToPage();
        }
        Db.PdvTerminais.Remove(terminal);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Excluiu terminal PDV #{id} - {terminal.Nome}");
        TempData["Success"] = $"Terminal \"{terminal.Nome}\" excluído.";
        return RedirectToPage();
    }
}
