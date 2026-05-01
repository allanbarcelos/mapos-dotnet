using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Produtos;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public List<Categoria> Categorias { get; set; } = [];

    public class InputModel
    {
        public int Id { get; set; }
        public string? CodDeBarra { get; set; }
        [Required] public string Descricao { get; set; } = string.Empty;
        public string? Unidade { get; set; }
        public decimal PrecoCompra { get; set; }
        public decimal PrecoVenda  { get; set; }
        public int Estoque { get; set; }
        public int EstoqueMinimo { get; set; }
        public bool Saida  { get; set; }
        public bool Entrada { get; set; }
        public int? CategoriaId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("eProduto")) return SemPermissao();
        var p = await Db.Produtos.FindAsync(id);
        if (p is null) return NotFound();
        Input = new InputModel { Id = p.Id, CodDeBarra = p.CodDeBarra, Descricao = p.Descricao, Unidade = p.Unidade, PrecoCompra = p.PrecoCompra, PrecoVenda = p.PrecoVenda, Estoque = p.Estoque, EstoqueMinimo = p.EstoqueMinimo, Saida = p.Saida, Entrada = p.Entrada, CategoriaId = p.CategoriaId };
        Categorias = await Db.Categorias.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eProduto")) return SemPermissao();
        if (!ModelState.IsValid) { Categorias = await Db.Categorias.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync(); await SetLayoutDataAsync(); return Page(); }
        var p = await Db.Produtos.FindAsync(Input.Id);
        if (p is null) return NotFound();
        p.CodDeBarra = Input.CodDeBarra; p.Descricao = Input.Descricao; p.Unidade = Input.Unidade;
        p.PrecoCompra = Input.PrecoCompra; p.PrecoVenda = Input.PrecoVenda;
        p.Estoque = Input.Estoque; p.EstoqueMinimo = Input.EstoqueMinimo;
        p.Saida = Input.Saida; p.Entrada = Input.Entrada; p.CategoriaId = Input.CategoriaId;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Produto atualizado.";
        return RedirectToPage("/Produtos/Index");
    }
}
