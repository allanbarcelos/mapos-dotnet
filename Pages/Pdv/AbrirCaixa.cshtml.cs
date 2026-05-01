using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Pdv;

public class AbrirCaixaModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<PdvTerminal> Terminais { get; private set; } = [];

    public class InputModel
    {
        [Required]
        [Range(0, 999999.99, ErrorMessage = "Informe um saldo válido.")]
        public decimal SaldoInicial { get; set; }

        public string? Observacoes { get; set; }

        [Required(ErrorMessage = "Selecione um terminal.")]
        public int PdvTerminalId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!SessaoPdv) return SomenteSessionPdv();
        if (!await TemPermissaoAsync("vPdv")) return SemPermissao();

        // Se já tem sessão aberta, redireciona para o PDV
        var sessaoAberta = await Db.SessoesCaixa
            .AnyAsync(s => s.OperadorId == UsuarioId && s.Status == "aberta");

        if (sessaoAberta)
            return RedirectToPage("Index");

        Terminais = await Db.PdvTerminais.Where(t => t.Status == "disponivel").OrderBy(t => t.Nome).ToListAsync();

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!SessaoPdv) return SomenteSessionPdv();
        if (!await TemPermissaoAsync("vPdv")) return SemPermissao();

        if (!ModelState.IsValid)
        {
            Terminais = await Db.PdvTerminais.Where(t => t.Status == "disponivel").OrderBy(t => t.Nome).ToListAsync();
            await SetLayoutDataAsync();
            return Page();
        }

        // Validate terminal is still available
        var terminal = await Db.PdvTerminais.FindAsync(Input.PdvTerminalId);
        if (terminal is null || terminal.Status != "disponivel")
        {
            ModelState.AddModelError("Input.PdvTerminalId", "Terminal não disponível. Selecione outro.");
            Terminais = await Db.PdvTerminais.Where(t => t.Status == "disponivel").OrderBy(t => t.Nome).ToListAsync();
            await SetLayoutDataAsync();
            return Page();
        }

        var sessao = new SessaoCaixa
        {
            AbertoEm      = DateTime.UtcNow,
            SaldoInicial  = Input.SaldoInicial,
            SaldoEsperado = Input.SaldoInicial,
            Status        = "aberta",
            Observacoes   = Input.Observacoes,
            OperadorId    = UsuarioId,
            PdvTerminalId = Input.PdvTerminalId,
        };

        await using var tx = await Db.Database.BeginTransactionAsync();
        Db.SessoesCaixa.Add(sessao);
        await Db.SaveChangesAsync();

        // Update terminal status
        terminal.Status = "ocupado";
        terminal.SessaoCaixaId = sessao.Id;
        await Db.SaveChangesAsync();
        await tx.CommitAsync();

        await AuditarAsync($"Abriu caixa PDV #{sessao.Id} com saldo inicial R${Input.SaldoInicial:F2} no terminal {terminal.Nome}");

        TempData["Success"] = $"Caixa aberto com saldo inicial de R${Input.SaldoInicial:F2}.";
        return RedirectToPage("Index");
    }
}
