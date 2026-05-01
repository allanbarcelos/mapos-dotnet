using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Contas;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IList<Conta> Contas { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vConta"))
            return SemPermissao();

        await SetLayoutDataAsync();
        PodeAdicionar = await TemPermissaoAsync("aConta");
        Contas = await Db.Contas.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dConta"))
            return SemPermissao();

        var conta = await Db.Contas.FindAsync(id);
        if (conta is not null)
        {
            Db.Contas.Remove(conta);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Excluiu conta #{id} - {conta.Nome}");
            TempData["Success"] = "Conta excluída com sucesso.";
        }

        return RedirectToPage();
    }
}
