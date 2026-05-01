using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Relatorios;

public class ServicosModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public record ServicoRow(Servico Servico, int Usos, decimal TotalFaturado);

    public List<ServicoRow> Items { get; set; } = [];
    public string? Q { get; set; }

    private async Task<bool> CarregarAsync(string? q)
    {
        if (!await TemPermissaoAsync("rServico")) return false;
        Q = q;

        var query = Db.Servicos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(s => s.Nome.ToLower().Contains(t));
        }
        var servicos = await query.OrderBy(s => s.Nome).ToListAsync();

        var usoGrupo = await Db.ServicosOs.AsNoTracking()
            .GroupBy(x => x.ServicoId)
            .Select(g => new { ServicoId = g.Key, Count = g.Count(), Total = g.Sum(x => x.SubTotal) })
            .ToListAsync();

        var usoDict = usoGrupo.ToDictionary(x => x.ServicoId);

        Items = servicos.Select(s => new ServicoRow(
            s,
            usoDict.TryGetValue(s.Id, out var u) ? u.Count : 0,
            usoDict.TryGetValue(s.Id, out var u2) ? u2.Total : 0
        )).ToList();

        return true;
    }

    public async Task<IActionResult> OnGetAsync(string? q)
    {
        if (!await CarregarAsync(q)) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportarAsync(string? q)
    {
        if (!await CarregarAsync(q)) return SemPermissao();

        string[] headers = ["#", "Nome", "Descrição", "Preço (R$)", "Usos em OS", "Total Faturado (R$)"];
        var rows = Items.Select(r => new object?[]
        {
            r.Servico.Id,
            r.Servico.Nome,
            r.Servico.Descricao ?? "",
            r.Servico.Preco,
            r.Usos,
            r.TotalFaturado
        });

        var bytes = ExcelHelper.Gerar("Servicos", headers, rows);
        return File(bytes, ExcelHelper.ContentType, "relatorio-servicos.xlsx");
    }
}
