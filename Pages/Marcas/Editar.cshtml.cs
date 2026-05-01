using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Marcas;

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
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        public bool Situacao { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("eMarca"))
            return SemPermissao();

        var marca = await Db.Marcas.FindAsync(Id);
        if (marca is null)
        {
            TempData["Error"] = "Marca não encontrada.";
            return RedirectToPage("Index");
        }

        Input = new InputModel
        {
            Nome     = marca.Nome,
            Situacao = marca.Situacao
        };

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eMarca"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var marca = await Db.Marcas.FindAsync(Id);
        if (marca is null)
        {
            TempData["Error"] = "Marca não encontrada.";
            return RedirectToPage("Index");
        }

        marca.Nome     = Input.Nome;
        marca.Situacao = Input.Situacao;

        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou marca #{marca.Id} - {marca.Nome}");

        TempData["Success"] = "Marca atualizada com sucesso.";
        return RedirectToPage("Index");
    }
}
