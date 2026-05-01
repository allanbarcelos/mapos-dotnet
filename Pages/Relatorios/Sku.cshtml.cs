using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Relatorios;

public class SkuModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public record SkuRow(
        int ClienteId,
        string NomeCliente,
        int ProdutoId,
        string Descricao,
        int Quantidade,
        int IdRelacionado,
        DateOnly DataOcorrencia,
        decimal Preco,
        decimal PrecoTotal,
        string Origem);   // "venda" | "os"

    public List<SkuRow> Items { get; set; } = [];
    public string? Q { get; set; }
    public string? De { get; set; }
    public string? Ate { get; set; }
    public string? Origem { get; set; }
    public decimal TotalGeral { get; set; }

    private async Task<bool> CarregarAsync(string? q, string? de, string? ate, string? origem)
    {
        if (!await TemPermissaoAsync("rVenda") || !await TemPermissaoAsync("rOs"))
            return false;

        Q = q; De = de; Ate = ate; Origem = origem;

        DateOnly? dtDe  = de  is not null ? DateOnly.TryParse(de,  out var d1) ? d1 : null : null;
        DateOnly? dtAte = ate is not null ? DateOnly.TryParse(ate, out var d2) ? d2 : null : null;

        var qLower = q?.Trim().ToLower();

        IQueryable<ItemVenda> iQuery = Db.ItensVenda.AsNoTracking()
            .Include(i => i.Venda).ThenInclude(v => v.Cliente)
            .Include(i => i.Produto);

        if (dtDe.HasValue)  iQuery = iQuery.Where(i => i.Venda.DataVenda >= dtDe.Value);
        if (dtAte.HasValue) iQuery = iQuery.Where(i => i.Venda.DataVenda <= dtAte.Value);
        if (!string.IsNullOrWhiteSpace(qLower))
            iQuery = iQuery.Where(i => i.Produto.Descricao.ToLower().Contains(qLower)
                                     || i.Venda.Cliente.NomeCliente.ToLower().Contains(qLower));

        IQueryable<ProdutoOs> pQuery = Db.ProdutosOs.AsNoTracking()
            .Include(p => p.Os).ThenInclude(o => o.Cliente)
            .Include(p => p.Produto);

        if (dtDe.HasValue)  pQuery = pQuery.Where(p => p.Os.DataInicial >= dtDe.Value);
        if (dtAte.HasValue) pQuery = pQuery.Where(p => p.Os.DataInicial <= dtAte.Value);
        if (!string.IsNullOrWhiteSpace(qLower))
            pQuery = pQuery.Where(p => p.Produto.Descricao.ToLower().Contains(qLower)
                                      || p.Os.Cliente.NomeCliente.ToLower().Contains(qLower));

        var vendasRows = origem is null or "venda"
            ? (await iQuery.ToListAsync())
                .Select(i => new SkuRow(
                    i.Venda.ClienteId,
                    i.Venda.Cliente?.NomeCliente ?? "—",
                    i.ProdutoId,
                    i.Produto?.Descricao ?? "—",
                    i.Quantidade,
                    i.VendaId,
                    i.Venda.DataVenda,
                    i.Preco,
                    i.SubTotal,
                    "venda"))
            : [];

        var osRows = origem is null or "os"
            ? (await pQuery.ToListAsync())
                .Select(p => new SkuRow(
                    p.Os.ClienteId,
                    p.Os.Cliente?.NomeCliente ?? "—",
                    p.ProdutoId,
                    p.Produto?.Descricao ?? "—",
                    p.Quantidade,
                    p.OsId,
                    p.Os.DataInicial ?? DateOnly.FromDateTime(DateTime.Today),
                    p.Preco,
                    p.SubTotal,
                    "os"))
            : [];

        Items = vendasRows.Concat(osRows)
            .OrderByDescending(r => r.DataOcorrencia)
            .Take(1000)
            .ToList();

        TotalGeral = Items.Sum(r => r.PrecoTotal);
        return true;
    }

    public async Task<IActionResult> OnGetAsync(string? q, string? de, string? ate, string? origem)
    {
        if (!await CarregarAsync(q, de, ate, origem)) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportarAsync(string? q, string? de, string? ate, string? origem)
    {
        if (!await CarregarAsync(q, de, ate, origem)) return SemPermissao();

        string[] headers = ["Origem", "#", "Data", "Cliente", "Produto", "Qtd", "Preço Unit. (R$)", "Total (R$)"];
        var rows = Items.Select(r => new object?[]
        {
            r.Origem,
            r.IdRelacionado,
            r.DataOcorrencia,
            r.NomeCliente,
            r.Descricao,
            r.Quantidade,
            r.Preco,
            r.PrecoTotal
        });

        var bytes = ExcelHelper.Gerar("SKU Produtos Vendidos", headers, rows);
        return File(bytes, ExcelHelper.ContentType, "relatorio-sku.xlsx");
    }
}
