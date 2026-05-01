using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Relatorios;

public class VendasModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<Venda> Items  { get; set; } = [];
    public string? Q          { get; set; }
    public string? Status     { get; set; }
    public string? De         { get; set; }
    public string? Ate        { get; set; }
    public decimal TotalGeral { get; set; }

    private async Task<bool> CarregarAsync(string? q, string? status, string? de, string? ate)
    {
        if (!await TemPermissaoAsync("rVenda")) return false;
        Q = q; Status = status; De = de; Ate = ate;

        var query = Db.Vendas.AsNoTracking()
            .Include(v => v.Cliente)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(v => v.Cliente.NomeCliente.ToLower().Contains(t));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(v => v.Status == status);
        if (DateOnly.TryParse(de,  out var d)) query = query.Where(v => v.DataVenda >= d);
        if (DateOnly.TryParse(ate, out var a)) query = query.Where(v => v.DataVenda <= a);

        Items      = await query.OrderByDescending(v => v.Id).Take(500).ToListAsync();
        TotalGeral = Items.Sum(v => v.ValorTotal);
        return true;
    }

    public async Task<IActionResult> OnGetAsync(string? q, string? status, string? de, string? ate)
    {
        if (!await CarregarAsync(q, status, de, ate)) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportarAsync(string? q, string? status, string? de, string? ate)
    {
        if (!await CarregarAsync(q, status, de, ate)) return SemPermissao();

        string[] headers = ["#", "Cliente", "Status", "Data", "Forma Pgto", "Faturado", "Total (R$)"];
        var rows = Items.Select(v => new object?[]
        {
            v.Id,
            v.Cliente?.NomeCliente ?? "",
            v.Status,
            v.DataVenda,
            v.FormaPagamento ?? "",
            v.Faturado,
            v.ValorTotal
        });

        var bytes = ExcelHelper.Gerar("Vendas", headers, rows);
        return File(bytes, ExcelHelper.ContentType, "relatorio-vendas.xlsx");
    }
}
