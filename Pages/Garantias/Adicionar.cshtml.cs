using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Garantias;

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
        [MaxLength(15)]
        public string? RefGarantia { get; set; }

        public DateOnly? DataGarantia { get; set; }

        [Required(ErrorMessage = "O texto da garantia é obrigatório.")]
        public string TextoGarantia { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aGarantia"))
            return SemPermissao();

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aGarantia"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var garantia = new Garantia
        {
            RefGarantia   = Input.RefGarantia,
            DataGarantia  = Input.DataGarantia,
            TextoGarantia = Input.TextoGarantia,
            UsuarioId     = UsuarioId
        };

        Db.Garantias.Add(garantia);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Adicionou garantia #{garantia.Id} - {garantia.RefGarantia}");

        TempData["Success"] = "Garantia cadastrada com sucesso.";
        return RedirectToPage("Index");
    }
}
