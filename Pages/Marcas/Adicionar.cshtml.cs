using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Marcas;

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
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        public bool Situacao { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aMarca"))
            return SemPermissao();

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aMarca"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var marca = new Marca
        {
            Nome     = Input.Nome,
            Situacao = Input.Situacao,
            Cadastro = DateOnly.FromDateTime(DateTime.Today)
        };

        Db.Marcas.Add(marca);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Adicionou marca #{marca.Id} - {marca.Nome}");

        TempData["Success"] = "Marca cadastrada com sucesso.";
        return RedirectToPage("Index");
    }
}
