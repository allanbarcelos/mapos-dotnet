using mapos_dotnet.Data;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages;

public class IndexModel : PageModelBase
{
    public IndexModel(
        ApplicationDbContext db,
        IPermissaoService permissaoSvc,
        IAuditService auditSvc,
        IConfiguracaoService configSvc)
        : base(db, permissaoSvc, auditSvc, configSvc) { }

    // Stats — only populated when the user has the respective permission
    public int TotalClientes { get; private set; }
    public int OsAbertas { get; private set; }
    public int LancamentosPendentes { get; private set; }
    public decimal VendasMes { get; private set; }

    // Resumo financeiro do mês
    public decimal ReceitaMes { get; private set; }
    public decimal DespesaMes { get; private set; }

    // Permission flags
    public bool PodeVerCliente { get; private set; }
    public bool PodeVerProduto { get; private set; }
    public bool PodeVerServico { get; private set; }
    public bool PodeVerOs { get; private set; }
    public bool PodeVerVenda { get; private set; }
    public bool PodeVerLancamento { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await SetLayoutDataAsync();

        // Resolve permissions first — queries are conditional on them
        PodeVerCliente    = await TemPermissaoAsync("vCliente");
        PodeVerProduto    = await TemPermissaoAsync("vProduto");
        PodeVerServico    = await TemPermissaoAsync("vServico");
        PodeVerOs         = await TemPermissaoAsync("vOs");
        PodeVerVenda      = await TemPermissaoAsync("vVenda");
        PodeVerLancamento = await TemPermissaoAsync("vLancamento");

        var agora = DateTime.Now;
        var inicioMes = new DateOnly(agora.Year, agora.Month, 1);
        var fimMes = inicioMes.AddMonths(1).AddDays(-1);

        if (PodeVerCliente)
            TotalClientes = await Db.Clientes.CountAsync();

        if (PodeVerOs)
            OsAbertas = await Db.Os.CountAsync(o => o.Status == "Aberto");

        if (PodeVerLancamento)
        {
            LancamentosPendentes = await Db.Lancamentos.CountAsync(l => !l.Baixado);
            ReceitaMes = await Db.Lancamentos
                             .Where(l => l.Tipo == "receita"
                                         && l.DataVencimento >= inicioMes
                                         && l.DataVencimento <= fimMes)
                             .SumAsync(l => (decimal?)l.Valor) ?? 0m;
            DespesaMes = await Db.Lancamentos
                             .Where(l => l.Tipo == "despesa"
                                         && l.DataVencimento >= inicioMes
                                         && l.DataVencimento <= fimMes)
                             .SumAsync(l => (decimal?)l.Valor) ?? 0m;
        }

        if (PodeVerVenda)
            VendasMes = await Db.Vendas
                            .Where(v => v.DataVenda >= inicioMes && v.DataVenda <= fimMes)
                            .SumAsync(v => (decimal?)v.ValorTotal) ?? 0m;

        return Page();
    }
}
