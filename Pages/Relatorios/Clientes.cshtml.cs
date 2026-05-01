using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Relatorios;

public class ClientesModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public record ClienteRow(Cliente Cliente, int TotalOs, int TotalVendas, decimal ValorOs, decimal ValorVendas);

    public List<ClienteRow> Items { get; set; } = [];
    public string? Q { get; set; }
    public string Tipo { get; set; } = "todos";

    private async Task<bool> CarregarAsync(string? q, string tipo)
    {
        if (!await TemPermissaoAsync("rCliente")) return false;
        Q = q; Tipo = tipo;

        var query = Db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(c => c.NomeCliente.ToLower().Contains(t)
                                  || (c.Documento != null && c.Documento.Contains(t)));
        }
        if (tipo == "cliente")    query = query.Where(c => !c.Fornecedor);
        if (tipo == "fornecedor") query = query.Where(c => c.Fornecedor);

        var clientes = await query.OrderBy(c => c.NomeCliente).Take(500).ToListAsync();

        var osGrupo = await Db.Os.AsNoTracking()
            .GroupBy(o => o.ClienteId)
            .Select(g => new { ClienteId = g.Key, Count = g.Count(), Total = g.Sum(x => x.ValorTotal) })
            .ToListAsync();

        var vendaGrupo = await Db.Vendas.AsNoTracking()
            .GroupBy(v => v.ClienteId)
            .Select(g => new { ClienteId = g.Key, Count = g.Count(), Total = g.Sum(x => x.ValorTotal) })
            .ToListAsync();

        var osDict    = osGrupo.ToDictionary(x => x.ClienteId);
        var vendaDict = vendaGrupo.ToDictionary(x => x.ClienteId);

        Items = clientes.Select(c => new ClienteRow(
            c,
            osDict.TryGetValue(c.Id, out var o) ? o.Count : 0,
            vendaDict.TryGetValue(c.Id, out var v) ? v.Count : 0,
            osDict.TryGetValue(c.Id, out var o2) ? o2.Total : 0,
            vendaDict.TryGetValue(c.Id, out var v2) ? v2.Total : 0
        )).ToList();

        return true;
    }

    public async Task<IActionResult> OnGetAsync(string? q, string tipo = "todos")
    {
        if (!await CarregarAsync(q, tipo)) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportarAsync(string? q, string tipo = "todos")
    {
        if (!await CarregarAsync(q, tipo)) return SemPermissao();

        string[] headers = ["#", "Nome", "Documento", "Cidade", "UF", "OS (qtd)", "Vendas (qtd)", "Total OS (R$)", "Total Vendas (R$)"];
        var rows = Items.Select(r => new object?[]
        {
            r.Cliente.Id,
            r.Cliente.NomeCliente,
            r.Cliente.Documento ?? "",
            r.Cliente.Cidade ?? "",
            r.Cliente.Estado ?? "",
            r.TotalOs,
            r.TotalVendas,
            r.ValorOs,
            r.ValorVendas
        });

        var bytes = ExcelHelper.Gerar("Clientes", headers, rows);
        return File(bytes, ExcelHelper.ContentType, "relatorio-clientes.xlsx");
    }
}
