using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Pdv;

public class PainelModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IReadOnlyList<TerminalInfo> Terminais { get; private set; } = [];
    public int TotalAbertos    { get; private set; }
    public int TotalPausados   { get; private set; }
    public int TotalDisponiveis{ get; private set; }
    public decimal TotalVendasDia { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vPdv")) return SemPermissao();
        await SetLayoutDataAsync();

        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var terminais = await Db.PdvTerminais
            .AsNoTracking()
            .Include(t => t.SessaoCaixa)
                .ThenInclude(s => s!.Operador)
            .Include(t => t.SessaoCaixa)
                .ThenInclude(s => s!.Vendas)
            .Include(t => t.SessaoCaixa)
                .ThenInclude(s => s!.Pausas)
            .OrderBy(t => t.Nome)
            .ToListAsync();

        var result = new List<TerminalInfo>();
        foreach (var t in terminais)
        {
            var sessao = t.SessaoCaixa;
            PdvPausa? pausaAtiva = null;
            if (sessao is not null)
            {
                pausaAtiva = sessao.Pausas
                    .Where(p => p.RetomadaEm == null)
                    .OrderByDescending(p => p.IniciadaEm)
                    .FirstOrDefault();
            }

            result.Add(new TerminalInfo(
                Terminal:    t,
                Sessao:      sessao,
                PausaAtiva:  pausaAtiva,
                QtdVendas:   sessao?.Vendas.Count ?? 0,
                TotalVendas: sessao?.Vendas.Sum(v => v.ValorTotal) ?? 0
            ));
        }

        Terminais        = result;
        TotalAbertos     = terminais.Count(t => t.Status == "ocupado");
        TotalPausados    = terminais.Count(t => t.Status == "pausado");
        TotalDisponiveis = terminais.Count(t => t.Status == "disponivel");

        // Total de vendas PDV hoje (todas as sessões)
        TotalVendasDia = await Db.Vendas
            .AsNoTracking()
            .Where(v => v.SessaoCaixaId != null && v.DataVenda == hoje)
            .SumAsync(v => v.ValorTotal);

        return Page();
    }

    public record TerminalInfo(
        PdvTerminal Terminal,
        SessaoCaixa? Sessao,
        PdvPausa? PausaAtiva,
        int QtdVendas,
        decimal TotalVendas);
}
