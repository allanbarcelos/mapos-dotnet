using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Produtos;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IReadOnlyList<Produto> Produtos { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public string? Q { get; private set; }
    public bool PodeAdicionar { get; private set; }

    private const int PageSize = 10;

    public async Task<IActionResult> OnGetAsync(string? q, int page = 1)
    {
        if (!await TemPermissaoAsync("vProduto"))
            return SemPermissao();

        await SetLayoutDataAsync();

        PodeAdicionar = await TemPermissaoAsync("aProduto");
        Q = q;
        CurrentPage = Math.Max(1, page);

        var query = Db.Produtos
            .Include(p => p.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var busca = q.Trim().ToLower();
            query = query.Where(p =>
                p.Descricao.ToLower().Contains(busca) ||
                (p.CodDeBarra != null && p.CodDeBarra.Contains(busca)));
        }

        var total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        CurrentPage = Math.Min(CurrentPage, Math.Max(1, TotalPages));

        Produtos = await query
            .OrderBy(p => p.Descricao)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dProduto"))
            return SemPermissao();

        var produto = await Db.Produtos.FindAsync(id);
        if (produto is null)
        {
            TempData["Error"] = "Produto não encontrado.";
            return RedirectToPage();
        }

        Db.Produtos.Remove(produto);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Excluiu produto #{id} - {produto.Descricao}");

        TempData["Success"] = $"Produto \"{produto.Descricao}\" excluído com sucesso.";
        return RedirectToPage();
    }
}
