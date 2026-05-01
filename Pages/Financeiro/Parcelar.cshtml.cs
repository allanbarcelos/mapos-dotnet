using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Financeiro;

public class ParcelarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public List<Cliente>   Clientes    { get; set; } = [];
    public List<Categoria> Categorias  { get; set; } = [];
    public List<Conta>     Contas      { get; set; } = [];

    public class InputModel
    {
        [Required] public string Descricao   { get; set; } = string.Empty;
        [Required, Range(0.01, double.MaxValue)] public decimal ValorTotal { get; set; }
        [Range(0, double.MaxValue)] public decimal Entrada { get; set; }
        [Required, Range(1, 360)] public int QtdParcelas   { get; set; } = 1;
        public decimal Desconto                            { get; set; }
        public string TipoDesconto                         { get; set; } = "real";
        [Required] public DateOnly DataPrimeiraParcela     { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddMonths(1);
        public int? ClienteId                              { get; set; }
        public int? CategoriaId                            { get; set; }
        public int? ContaId                                { get; set; }
        public string? FormaPgto                           { get; set; }
        public string Tipo                                 { get; set; } = "receita";
        public string? Observacoes                         { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aLancamento")) return SemPermissao();
        await SetLayoutDataAsync();
        await CarregarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aLancamento")) return SemPermissao();
        if (!ModelState.IsValid) { await CarregarAsync(); await SetLayoutDataAsync(); return Page(); }

        var desconto = Input.TipoDesconto == "percent"
            ? Math.Round(Input.ValorTotal * Input.Desconto / 100, 2)
            : Input.Desconto;

        var valorLiquido = Input.ValorTotal - desconto;
        var resto        = valorLiquido - Input.Entrada;

        if (resto < 0)
        {
            ModelState.AddModelError("Input.Entrada", "A entrada não pode ser maior que o valor total.");
            await CarregarAsync(); await SetLayoutDataAsync(); return Page();
        }

        var valorParcela       = resto > 0 ? Math.Round(resto / Input.QtdParcelas, 2) : 0;
        var descontoPorParcela = desconto > 0 ? Math.Round(desconto / (Input.QtdParcelas + (Input.Entrada > 0 ? 1 : 0)), 2) : 0;

        // Entry installment
        if (Input.Entrada > 0)
        {
            Db.Lancamentos.Add(new Lancamento
            {
                Descricao      = $"{Input.Descricao} — Entrada do parcelamento de R${Input.ValorTotal:F2}",
                Valor          = Input.Entrada + descontoPorParcela,
                Desconto       = descontoPorParcela,
                ValorDesconto  = Input.Entrada,
                TipoDesconto   = "real",
                DataVencimento = DateOnly.FromDateTime(DateTime.Today),
                Tipo           = Input.Tipo,
                ClienteId      = Input.ClienteId,
                CategoriaId    = Input.CategoriaId,
                ContaId        = Input.ContaId,
                FormaPgto      = Input.FormaPgto,
                Observacoes    = Input.Observacoes,
                UsuarioId      = UsuarioId,
            });
        }

        // Monthly installments
        for (int i = 1; i <= Input.QtdParcelas; i++)
        {
            var vencimento = Input.DataPrimeiraParcela.AddMonths(i - 1);
            Db.Lancamentos.Add(new Lancamento
            {
                Descricao      = $"{Input.Descricao} — Parc. R${Input.ValorTotal:F2} [{i}/{Input.QtdParcelas}]",
                Valor          = valorParcela + descontoPorParcela,
                Desconto       = descontoPorParcela,
                ValorDesconto  = valorParcela,
                TipoDesconto   = "real",
                DataVencimento = vencimento,
                Tipo           = Input.Tipo,
                ClienteId      = Input.ClienteId,
                CategoriaId    = Input.CategoriaId,
                ContaId        = Input.ContaId,
                FormaPgto      = Input.FormaPgto,
                Observacoes    = Input.Observacoes,
                UsuarioId      = UsuarioId,
            });
        }

        await Db.SaveChangesAsync();
        await AuditarAsync($"Parcelamento: {Input.Descricao} — R${Input.ValorTotal:F2} em {Input.QtdParcelas}x");
        TempData["Success"] = $"Parcelamento criado: {Input.QtdParcelas} parcela(s){(Input.Entrada > 0 ? " + entrada" : "")}.";
        return RedirectToPage("/Financeiro/Lancamentos");
    }

    private async Task CarregarAsync()
    {
        Clientes   = await Db.Clientes.OrderBy(c => c.NomeCliente).ToListAsync();
        Categorias = await Db.Categorias.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
        Contas     = await Db.Contas.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
    }
}
