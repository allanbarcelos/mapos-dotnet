using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Servicos;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IList<Servico> Servicos { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vServico"))
            return SemPermissao();

        await SetLayoutDataAsync();
        PodeAdicionar = await TemPermissaoAsync("aServico");
        Servicos = await Db.Servicos.AsNoTracking().OrderBy(s => s.Nome).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dServico"))
            return SemPermissao();

        var servico = await Db.Servicos.FindAsync(id);
        if (servico is not null)
        {
            Db.Servicos.Remove(servico);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Excluiu serviço #{id} - {servico.Nome}");
            TempData["Success"] = "Serviço excluído com sucesso.";
        }

        return RedirectToPage();
    }
}
