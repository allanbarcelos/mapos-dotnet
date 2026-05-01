using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Produtos;

public class VisualizarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Produto Produto { get; set; } = null!;
    public List<ProdutoOs>  UsoOs    { get; set; } = [];
    public List<ItemVenda>  UsoVenda { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vProduto")) return SemPermissao();
        await SetLayoutDataAsync();

        var p = await Db.Produtos.AsNoTracking()
            .Include(x => x.Categoria)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        Produto = p;

        UsoOs = await Db.ProdutosOs.AsNoTracking()
            .Include(x => x.Os).ThenInclude(o => o.Cliente)
            .Where(x => x.ProdutoId == id)
            .OrderByDescending(x => x.Id).Take(50).ToListAsync();

        UsoVenda = await Db.ItensVenda.AsNoTracking()
            .Include(x => x.Venda).ThenInclude(v => v.Cliente)
            .Where(x => x.ProdutoId == id)
            .OrderByDescending(x => x.Id).Take(50).ToListAsync();

        return Page();
    }
}
