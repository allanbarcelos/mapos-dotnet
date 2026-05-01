using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Relatorios;

public class OsModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<Models.Os> Items { get; set; } = [];
    public string? Q        { get; set; }
    public string? Status   { get; set; }
    public string? De       { get; set; }
    public string? Ate      { get; set; }
    public decimal TotalGeral { get; set; }

    private async Task<bool> CarregarAsync(string? q, string? status, string? de, string? ate)
    {
        if (!await TemPermissaoAsync("rOs")) return false;
        Q = q; Status = status; De = de; Ate = ate;

        var query = Db.Os.AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(o => o.Cliente.NomeCliente.ToLower().Contains(t)
                                  || (o.Defeito != null && o.Defeito.ToLower().Contains(t)));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
        if (DateOnly.TryParse(de,  out var d)) query = query.Where(o => o.DataInicial >= d);
        if (DateOnly.TryParse(ate, out var a)) query = query.Where(o => o.DataInicial <= a);

        Items      = await query.OrderByDescending(o => o.Id).Take(500).ToListAsync();
        TotalGeral = Items.Sum(o => o.ValorTotal);
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

        string[] headers = ["#", "Cliente", "Técnico", "Status", "Produto/Equipamento", "Defeito", "Abertura", "Encerramento", "Faturado", "Total (R$)"];
        var rows = Items.Select(o => new object?[]
        {
            o.Id,
            o.Cliente.NomeCliente,
            o.Usuario.Nome,
            o.Status,
            o.DescricaoProduto ?? "",
            o.Defeito ?? "",
            o.DataInicial,
            o.DataFinal,
            o.Faturado,
            o.ValorTotal
        });

        var bytes = ExcelHelper.Gerar("Ordens de Servico", headers, rows);
        return File(bytes, ExcelHelper.ContentType, "relatorio-os.xlsx");
    }
}
