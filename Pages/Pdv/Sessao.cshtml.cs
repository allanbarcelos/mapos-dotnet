using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Pdv;

public class SessaoModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IPdvPrinterService printer)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public SessaoCaixa Sessao { get; set; } = null!;
    public IReadOnlyList<PdvPausa> Pausas { get; private set; } = [];
    public decimal TotalVendas { get; set; }
    public decimal TotalDinheiro { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalDebito { get; set; }
    public decimal TotalCredito { get; set; }
    public decimal TotalFiado { get; set; }
    public int QtdVendas { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vPdv")) return SemPermissao();

        var sessao = await Db.SessoesCaixa
            .Include(s => s.Operador)
            .Include(s => s.Vendas.OrderByDescending(v => v.DataVenda))
            .Include(s => s.Pausas.OrderBy(p => p.IniciadaEm))
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sessao is null) return NotFound();

        // Only own sessions (unless admin permission allows viewing all - for now restrict to own)
        if (sessao.OperadorId != UsuarioId)
            return SemPermissao();

        Sessao = sessao;
        Pausas = sessao.Pausas.OrderBy(p => p.IniciadaEm).ToList();

        QtdVendas     = sessao.Vendas.Count;
        TotalVendas   = sessao.Vendas.Sum(v => v.ValorTotal);
        TotalDinheiro = sessao.Vendas.Where(v => v.FormaPagamento == "dinheiro").Sum(v => v.ValorTotal);
        TotalPix      = sessao.Vendas.Where(v => v.FormaPagamento == "pix").Sum(v => v.ValorTotal);
        TotalDebito   = sessao.Vendas.Where(v => v.FormaPagamento == "debito").Sum(v => v.ValorTotal);
        TotalCredito  = sessao.Vendas.Where(v => v.FormaPagamento == "credito").Sum(v => v.ValorTotal);
        TotalFiado    = sessao.Vendas.Where(v => v.FormaPagamento == "fiado").Sum(v => v.ValorTotal);

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostImprimirExtratoAsync(int id)
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        var pertence = await Db.SessoesCaixa.AsNoTracking()
            .AnyAsync(s => s.Id == id && s.OperadorId == UsuarioId);
        if (!pertence)
            return new JsonResult(new { ok = false, erro = "Sessão não encontrada." });

        var (ok, erro) = await printer.ImprimirExtratoAsync(id);
        return new JsonResult(new { ok, erro });
    }

    public async Task<IActionResult> OnPostImprimirRelatorioAsync(int id)
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        var pertence = await Db.SessoesCaixa.AsNoTracking()
            .AnyAsync(s => s.Id == id && s.OperadorId == UsuarioId);
        if (!pertence)
            return new JsonResult(new { ok = false, erro = "Sessão não encontrada." });

        var (ok, erro) = await printer.ImprimirRelatorioAsync(id);
        return new JsonResult(new { ok, erro });
    }
}
