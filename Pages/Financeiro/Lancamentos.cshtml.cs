using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Financeiro;

public class LancamentosModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<Lancamento> Items { get; set; } = [];
    public int CurrentPage  { get; set; } = 1;
    public int TotalPages   { get; set; } = 1;
    public string? Q        { get; set; }
    public string Tipo      { get; set; } = "all";
    public string Situacao  { get; set; } = "all";

    public async Task<IActionResult> OnGetAsync(string? q, string tipo = "all", string situacao = "all", int page = 1)
    {
        if (!await TemPermissaoAsync("vLancamento")) return SemPermissao();
        await SetLayoutDataAsync();
        Q = q; Tipo = tipo; Situacao = situacao; CurrentPage = page;

        var query = Db.Lancamentos.AsNoTracking().Include(l => l.Conta).Include(l => l.Categoria).AsQueryable();
        if (!string.IsNullOrEmpty(q))
            query = query.Where(l => l.Descricao.Contains(q) || (l.ClienteFornecedor != null && l.ClienteFornecedor.Contains(q)));
        if (tipo != "all") query = query.Where(l => l.Tipo == tipo);
        if (situacao == "pendente") query = query.Where(l => !l.Baixado);
        if (situacao == "pago")     query = query.Where(l => l.Baixado);

        var per = int.TryParse(await ConfigSvc.GetAsync("per_page"), out var pp) ? pp : 10;
        var total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / (double)per);
        Items = await query.OrderByDescending(l => l.Id).Skip((page - 1) * per).Take(per).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostBaixarAsync(int id)
    {
        if (!await TemPermissaoAsync("eLancamento")) return SemPermissao();
        var l = await Db.Lancamentos.FindAsync(id);
        if (l is not null)
        {
            l.Baixado       = true;
            l.DataPagamento = DateOnly.FromDateTime(DateTime.Today);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Baixou Lançamento #{id}");
        }
        TempData["Success"] = "Lançamento baixado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dLancamento")) return SemPermissao();
        var l = await Db.Lancamentos.FindAsync(id);
        if (l is not null) Db.Lancamentos.Remove(l);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Lançamento excluído.";
        return RedirectToPage();
    }
}
