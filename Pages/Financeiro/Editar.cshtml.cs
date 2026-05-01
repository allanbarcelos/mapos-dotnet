using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Financeiro;

public class EditarModel(
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
        public int Id { get; set; }
        [Required] public string Descricao { get; set; } = string.Empty;
        [Required] public decimal Valor { get; set; }
        public decimal Desconto { get; set; }
        public string TipoDesconto { get; set; } = "real";
        [Required] public DateOnly DataVencimento { get; set; }
        public DateOnly? DataPagamento { get; set; }
        public bool Baixado { get; set; }
        public string Tipo { get; set; } = "receita";
        public string? ClienteFornecedor { get; set; }
        public int? ClienteId { get; set; }
        public int? CategoriaId { get; set; }
        public int? ContaId { get; set; }
        public string? FormaPgto { get; set; }
        public string? Observacoes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("eLancamento")) return SemPermissao();
        var l = await Db.Lancamentos.FindAsync(id);
        if (l is null) return NotFound();
        Input = new InputModel { Id = l.Id, Descricao = l.Descricao, Valor = l.Valor, Desconto = l.Desconto, TipoDesconto = l.TipoDesconto, DataVencimento = l.DataVencimento, DataPagamento = l.DataPagamento, Baixado = l.Baixado, Tipo = l.Tipo, ClienteFornecedor = l.ClienteFornecedor, ClienteId = l.ClienteId, CategoriaId = l.CategoriaId, ContaId = l.ContaId, FormaPgto = l.FormaPgto, Observacoes = l.Observacoes };
        await CarregarAsync();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eLancamento")) return SemPermissao();
        if (!ModelState.IsValid) { await CarregarAsync(); await SetLayoutDataAsync(); return Page(); }
        var l = await Db.Lancamentos.FindAsync(Input.Id);
        if (l is null) return NotFound();
        var desc = Input.TipoDesconto == "percent" ? Math.Round(Input.Valor * Input.Desconto / 100, 2) : Input.Desconto;
        l.Descricao = Input.Descricao; l.Valor = Input.Valor; l.Desconto = Input.Desconto;
        l.ValorDesconto = Input.Valor - desc; l.TipoDesconto = Input.TipoDesconto;
        l.DataVencimento = Input.DataVencimento; l.DataPagamento = Input.DataPagamento;
        l.Baixado = Input.Baixado; l.ClienteFornecedor = Input.ClienteFornecedor;
        l.ClienteId = Input.ClienteId; l.CategoriaId = Input.CategoriaId;
        l.ContaId = Input.ContaId; l.FormaPgto = Input.FormaPgto; l.Observacoes = Input.Observacoes;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Lançamento atualizado.";
        return RedirectToPage("/Financeiro/Lancamentos");
    }

    private async Task CarregarAsync()
    {
        Clientes   = await Db.Clientes.OrderBy(c => c.NomeCliente).ToListAsync();
        Categorias = await Db.Categorias.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
        Contas     = await Db.Contas.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
    }
}
