using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Categorias;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

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
        if (!await TemPermissaoAsync("eCategoria"))
            return SemPermissao();

        var categoria = await Db.Categorias.FindAsync(Id);
        if (categoria is null)
        {
            TempData["Error"] = "Categoria não encontrada.";
            return RedirectToPage("Index");
        }

        Input = new InputModel
        {
            Nome   = categoria.Nome,
            Tipo   = categoria.Tipo,
            Status = categoria.Status
        };

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eCategoria"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var categoria = await Db.Categorias.FindAsync(Id);
        if (categoria is null)
        {
            TempData["Error"] = "Categoria não encontrada.";
            return RedirectToPage("Index");
        }

        categoria.Nome   = Input.Nome;
        categoria.Tipo   = Input.Tipo;
        categoria.Status = Input.Status;

        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou categoria #{categoria.Id} - {categoria.Nome}");

        TempData["Success"] = "Categoria atualizada com sucesso.";
        return RedirectToPage("Index");
    }
}
