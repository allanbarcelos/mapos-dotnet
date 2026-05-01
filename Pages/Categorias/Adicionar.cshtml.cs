using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Categorias;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(80)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? Tipo { get; set; }

        public bool Status { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aCategoria"))
            return SemPermissao();

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aCategoria"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var categoria = new Categoria
        {
            Nome     = Input.Nome,
            Tipo     = Input.Tipo,
            Status   = Input.Status,
            Cadastro = DateOnly.FromDateTime(DateTime.Today)
        };

        Db.Categorias.Add(categoria);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Adicionou categoria #{categoria.Id} - {categoria.Nome}");

        TempData["Success"] = "Categoria cadastrada com sucesso.";
        return RedirectToPage("Index");
    }
}
