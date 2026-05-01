using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Produtos;

public class AdicionarModel(
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
        public string? CodDeBarra { get; set; }
        [Required] public string Descricao { get; set; } = string.Empty;
        public string? Unidade { get; set; }
        [Range(0, double.MaxValue)] public decimal PrecoCompra { get; set; }
        [Range(0, double.MaxValue)] public decimal PrecoVenda  { get; set; }
        public int Estoque { get; set; }
        public int EstoqueMinimo { get; set; }
        public bool Saida  { get; set; } = true;
        public bool Entrada { get; set; } = true;
        public int? CategoriaId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aProduto")) return SemPermissao();
        await SetLayoutDataAsync();
        Categorias = await Db.Categorias.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aProduto")) return SemPermissao();
        if (!ModelState.IsValid) { Categorias = await Db.Categorias.Where(c => c.Status).OrderBy(c => c.Nome).ToListAsync(); await SetLayoutDataAsync(); return Page(); }
        Db.Produtos.Add(new Produto { CodDeBarra = Input.CodDeBarra, Descricao = Input.Descricao, Unidade = Input.Unidade, PrecoCompra = Input.PrecoCompra, PrecoVenda = Input.PrecoVenda, Estoque = Input.Estoque, EstoqueMinimo = Input.EstoqueMinimo, Saida = Input.Saida, Entrada = Input.Entrada, CategoriaId = Input.CategoriaId });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Produto cadastrado.";
        return RedirectToPage("/Produtos/Index");
    }
}
