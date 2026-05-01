using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Pdv;

public class FecharCaixaModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public SessaoCaixa Sessao { get; set; } = null!;
    public decimal TotalVendas { get; set; }
    public decimal TotalDinheiro { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalDebito { get; set; }
    public decimal TotalCredito { get; set; }
    public decimal TotalFiado { get; set; }
    public int QtdVendas { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Range(0, 999999.99, ErrorMessage = "Informe um saldo válido.")]
        public decimal SaldoInformado { get; set; }

        public string? Observacoes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!SessaoPdv) return SomenteSessionPdv();
        if (!await TemPermissaoAsync("fPdv")) return SemPermissao();

        var sessao = await Db.SessoesCaixa
            .Include(s => s.Operador)
            .Include(s => s.Vendas)
            .Include(s => s.PdvTerminal)
            .FirstOrDefaultAsync(s => s.Id == id && s.OperadorId == UsuarioId);

        if (sessao is null) return NotFound();
        if (sessao.Status != "aberta")
            return RedirectToPage("Sessao", new { id });

        Sessao = sessao;
        CalcularTotais(sessao.Vendas);

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!SessaoPdv) return SomenteSessionPdv();
        if (!await TemPermissaoAsync("fPdv")) return SemPermissao();

        var sessao = await Db.SessoesCaixa
            .Include(s => s.Vendas)
            .Include(s => s.PdvTerminal)
            .FirstOrDefaultAsync(s => s.Id == id && s.OperadorId == UsuarioId);

        if (sessao is null) return NotFound();
        if (sessao.Status != "aberta")
            return RedirectToPage("Sessao", new { id });

        CalcularTotais(sessao.Vendas);

        sessao.FechadoEm       = DateTime.UtcNow;
        sessao.SaldoInformado  = Input.SaldoInformado;
        sessao.Diferenca       = Input.SaldoInformado - sessao.SaldoEsperado;
        sessao.Status          = "fechada";
        if (!string.IsNullOrWhiteSpace(Input.Observacoes))
            sessao.Observacoes = Input.Observacoes;

        // Libera o terminal
        if (sessao.PdvTerminal is not null)
        {
            sessao.PdvTerminal.Status = "disponivel";
            sessao.PdvTerminal.SessaoCaixaId = null;
        }

        await Db.SaveChangesAsync();
        await AuditarAsync($"Fechou caixa PDV #{sessao.Id} — saldo informado R${Input.SaldoInformado:F2}, diferença R${sessao.Diferenca:F2}");

        TempData["Success"] = $"Caixa #{sessao.Id} fechado com sucesso.";
        return RedirectToPage("Sessao", new { id });
    }

    private void CalcularTotais(ICollection<Venda> vendas)
    {
        QtdVendas     = vendas.Count;
        TotalVendas   = vendas.Sum(v => v.ValorTotal);
        TotalDinheiro = vendas.Where(v => v.FormaPagamento == "dinheiro").Sum(v => v.ValorTotal);
        TotalPix      = vendas.Where(v => v.FormaPagamento == "pix").Sum(v => v.ValorTotal);
        TotalDebito   = vendas.Where(v => v.FormaPagamento == "debito").Sum(v => v.ValorTotal);
        TotalCredito  = vendas.Where(v => v.FormaPagamento == "credito").Sum(v => v.ValorTotal);
        TotalFiado    = vendas.Where(v => v.FormaPagamento == "fiado").Sum(v => v.ValorTotal);
    }
}
