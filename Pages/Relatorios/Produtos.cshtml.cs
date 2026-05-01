using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Relatorios;

public class ProdutosModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public record ProdutoRow(Produto Produto, int UsadoOs, int UsadoVendas, decimal TotalVendido);

    public List<ProdutoRow> Items { get; set; } = [];
    public string? Q { get; set; }
    public bool ApenasEstoqueBaixo { get; set; }

    private async Task<bool> CarregarAsync(string? q, bool baixo)
    {
        if (!await TemPermissaoAsync("rProduto")) return false;
        Q = q; ApenasEstoqueBaixo = baixo;

        var query = Db.Produtos.AsNoTracking().Include(p => p.Categoria).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(p => p.Descricao.ToLower().Contains(t)
                                  || (p.CodDeBarra != null && p.CodDeBarra.Contains(t)));
        }
        if (baixo) query = query.Where(p => p.Estoque <= p.EstoqueMinimo);

        var produtos = await query.OrderBy(p => p.Descricao).Take(500).ToListAsync();

        var osGrupo = await Db.ProdutosOs.AsNoTracking()
            .GroupBy(x => x.ProdutoId)
            .Select(g => new { ProdutoId = g.Key, Count = g.Count(), Total = g.Sum(x => x.SubTotal) })
            .ToListAsync();

        var vendaGrupo = await Db.ItensVenda.AsNoTracking()
            .GroupBy(x => x.ProdutoId)
            .Select(g => new { ProdutoId = g.Key, Count = g.Count(), Total = g.Sum(x => x.SubTotal) })
            .ToListAsync();

        var osDict    = osGrupo.ToDictionary(x => x.ProdutoId);
        var vendaDict = vendaGrupo.ToDictionary(x => x.ProdutoId);

        Items = produtos.Select(p => new ProdutoRow(
            p,
            osDict.TryGetValue(p.Id, out var o) ? o.Count : 0,
            vendaDict.TryGetValue(p.Id, out var v) ? v.Count : 0,
            (osDict.TryGetValue(p.Id, out var o2) ? o2.Total : 0) +
            (vendaDict.TryGetValue(p.Id, out var v2) ? v2.Total : 0)
        )).ToList();

        return true;
    }

    public async Task<IActionResult> OnGetAsync(string? q, bool baixo = false)
    {
        if (!await CarregarAsync(q, baixo)) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportarAsync(string? q, bool baixo = false)
    {
        if (!await CarregarAsync(q, baixo)) return SemPermissao();

        string[] headers = ["#", "Descrição", "Cód. Barras", "Categoria", "Preço Compra (R$)", "Preço Venda (R$)", "Estoque", "Est. Mínimo", "Uso OS", "Uso Vendas", "Total Faturado (R$)"];
        var rows = Items.Select(r => new object?[]
        {
            r.Produto.Id,
            r.Produto.Descricao,
            r.Produto.CodDeBarra ?? "",
            r.Produto.Categoria?.Nome ?? "",
            r.Produto.PrecoCompra,
            r.Produto.PrecoVenda,
            r.Produto.Estoque,
            r.Produto.EstoqueMinimo,
            r.UsadoOs,
            r.UsadoVendas,
            r.TotalVendido
        });

        var bytes = ExcelHelper.Gerar("Produtos", headers, rows);
        return File(bytes, ExcelHelper.ContentType, "relatorio-produtos.xlsx");
    }
}
