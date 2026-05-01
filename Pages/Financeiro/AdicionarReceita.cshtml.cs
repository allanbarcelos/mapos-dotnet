using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Financeiro;

public class AdicionarReceitaModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public List<Cliente> Clientes    { get; set; } = [];
    public List<Categoria> Categorias { get; set; } = [];
    public List<Conta> Contas        { get; set; } = [];

    public class InputModel
    {
        [Required] public string Descricao    { get; set; } = string.Empty;
        [Required, Range(0.01, double.MaxValue)] public decimal Valor { get; set; }
        public decimal Desconto               { get; set; }
        public string TipoDesconto            { get; set; } = "real";
        [Required] public DateOnly DataVencimento { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public int? ClienteId                 { get; set; }
        public int? CategoriaId               { get; set; }
        public int? ContaId                   { get; set; }
        public string? FormaPgto              { get; set; }
        public string? Observacoes            { get; set; }
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
            ? Math.Round(Input.Valor * Input.Desconto / 100, 2)
            : Input.Desconto;

        Db.Lancamentos.Add(new Lancamento
        {
            Descricao      = Input.Descricao,
            Valor          = Input.Valor,
            Desconto       = Input.Desconto,
            ValorDesconto  = Input.Valor - desconto,
            TipoDesconto   = Input.TipoDesconto,
            DataVencimento = Input.DataVencimento,
            Tipo           = "receita",
            ClienteId      = Input.ClienteId,
            CategoriaId    = Input.CategoriaId,
            ContaId        = Input.ContaId,
            FormaPgto      = Input.FormaPgto,
            Observacoes    = Input.Observacoes,
            UsuarioId      = UsuarioId,
        });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Receita adicionada.";
        return RedirectToPage("/Financeiro/Lancamentos");
    }

    private async Task CarregarAsync()
    {
        Clientes   = await Db.Clientes.OrderBy(c => c.NomeCliente).ToListAsync();
        Categorias = await Db.Categorias.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
        Contas     = await Db.Contas.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
    }
}
