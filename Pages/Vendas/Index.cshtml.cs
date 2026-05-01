using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Vendas;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IReadOnlyList<Venda> Vendas { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public string? Q { get; private set; }
    public string? FiltroStatus { get; private set; }
    public DateOnly? FiltroDe { get; private set; }
    public DateOnly? FiltroAte { get; private set; }
    public bool PodeAdicionar { get; private set; }
    public bool PodeExcluir { get; private set; }

    private const int PageSize = 15;

    public async Task<IActionResult> OnGetAsync(
        string? q, string? status, string? de, string? ate, int page = 1)
    {
        if (!await TemPermissaoAsync("vVenda"))
            return SemPermissao();

        await SetLayoutDataAsync();

        PodeAdicionar = await TemPermissaoAsync("aVenda");
        PodeExcluir   = await TemPermissaoAsync("dVenda");
        Q             = q;
        FiltroStatus  = status;
        FiltroDe      = DateOnly.TryParse(de, out var d) ? d : null;
        FiltroAte     = DateOnly.TryParse(ate, out var a) ? a : null;
        CurrentPage   = Math.Max(1, page);

        var query = Db.Vendas
            .Include(v => v.Cliente)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var busca = q.Trim().ToLower();
            query = query.Where(v => v.Cliente.NomeCliente.ToLower().Contains(busca));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status);

        if (FiltroDe.HasValue)
            query = query.Where(v => v.DataVenda >= FiltroDe.Value);

        if (FiltroAte.HasValue)
            query = query.Where(v => v.DataVenda <= FiltroAte.Value);

        var total = await query.CountAsync();
        TotalPages  = (int)Math.Ceiling(total / (double)PageSize);
        CurrentPage = Math.Min(CurrentPage, Math.Max(1, TotalPages));

        Vendas = await query
            .OrderByDescending(v => v.DataVenda)
            .ThenByDescending(v => v.Id)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .AsNoTracking()
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dVenda"))
            return SemPermissao();

        var venda = await Db.Vendas.FindAsync(id);
        if (venda is null)
        {
            TempData["Error"] = "Venda não encontrada.";
            return RedirectToPage();
        }

        if (venda.Faturado)
        {
            TempData["Error"] = "Não é possível excluir uma venda já faturada.";
            return RedirectToPage();
        }

        Db.Vendas.Remove(venda);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Excluiu venda #{id}");

        TempData["Success"] = $"Venda #{id} excluída com sucesso.";
        return RedirectToPage();
    }
}
