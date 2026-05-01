using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Financeiro;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public decimal TotalReceitas    { get; set; }
    public decimal TotalDespesas    { get; set; }
    public decimal Saldo            { get; set; }
    public decimal AReceber         { get; set; }
    public decimal APagar           { get; set; }
    public List<Conta> Contas       { get; set; } = [];
    public List<Lancamento> Recentes { get; set; } = [];
    public int Mes  { get; set; }
    public int Ano  { get; set; }

    public async Task<IActionResult> OnGetAsync(int? mes, int? ano)
    {
        if (!await TemPermissaoAsync("vLancamento")) return SemPermissao();
        await SetLayoutDataAsync();

        Mes = mes ?? DateTime.Today.Month;
        Ano = ano ?? DateTime.Today.Year;
        var ini = new DateOnly(Ano, Mes, 1);
        var fim = ini.AddMonths(1).AddDays(-1);

        var q = Db.Lancamentos.AsNoTracking().Where(l => l.DataVencimento >= ini && l.DataVencimento <= fim);
        TotalReceitas = await q.Where(l => l.Tipo == "receita" && l.Baixado).SumAsync(l => (decimal?)l.ValorDesconto) ?? 0;
        TotalDespesas = await q.Where(l => l.Tipo == "despesa" && l.Baixado).SumAsync(l => (decimal?)l.ValorDesconto) ?? 0;
        Saldo         = TotalReceitas - TotalDespesas;
        AReceber      = await q.Where(l => l.Tipo == "receita" && !l.Baixado).SumAsync(l => (decimal?)l.ValorDesconto) ?? 0;
        APagar        = await q.Where(l => l.Tipo == "despesa" && !l.Baixado).SumAsync(l => (decimal?)l.ValorDesconto) ?? 0;

        Contas  = await Db.Contas.AsNoTracking().Where(c => c.Status).ToListAsync();
        Recentes = await Db.Lancamentos.AsNoTracking()
            .OrderByDescending(l => l.Id).Take(10).ToListAsync();
        return Page();
    }
}
