using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Garantias;

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
        [MaxLength(15)]
        public string? RefGarantia { get; set; }

        public DateOnly? DataGarantia { get; set; }

        [Required(ErrorMessage = "O texto da garantia é obrigatório.")]
        public string TextoGarantia { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("eGarantia"))
            return SemPermissao();

        var garantia = await Db.Garantias.FindAsync(Id);
        if (garantia is null)
        {
            TempData["Error"] = "Garantia não encontrada.";
            return RedirectToPage("Index");
        }

        Input = new InputModel
        {
            RefGarantia   = garantia.RefGarantia,
            DataGarantia  = garantia.DataGarantia,
            TextoGarantia = garantia.TextoGarantia
        };

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eGarantia"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var garantia = await Db.Garantias.FindAsync(Id);
        if (garantia is null)
        {
            TempData["Error"] = "Garantia não encontrada.";
            return RedirectToPage("Index");
        }

        garantia.RefGarantia   = Input.RefGarantia;
        garantia.DataGarantia  = Input.DataGarantia;
        garantia.TextoGarantia = Input.TextoGarantia;

        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou garantia #{garantia.Id} - {garantia.RefGarantia}");

        TempData["Success"] = "Garantia atualizada com sucesso.";
        return RedirectToPage("Index");
    }
}
