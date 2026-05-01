using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.MinhaConta;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public SenhaInput Senha { get; set; } = new();
    public Models.Usuario? Usuario { get; set; }

    public class SenhaInput
    {
        [Required] public string Atual { get; set; } = string.Empty;
        [Required, MinLength(6)] public string Nova { get; set; } = string.Empty;
        [Compare(nameof(Nova), ErrorMessage = "As senhas não conferem")] public string Confirmar { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await SetLayoutDataAsync();
        Usuario = await Db.Usuarios.FindAsync(UsuarioId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await SetLayoutDataAsync();
        Usuario = await Db.Usuarios.FindAsync(UsuarioId);
        if (Usuario is null) return NotFound();
        if (!ModelState.IsValid) return Page();

        if (!BCrypt.Net.BCrypt.Verify(Senha.Atual, Usuario.Senha))
        {
            ModelState.AddModelError(nameof(Senha.Atual), "Senha atual incorreta.");
            return Page();
        }

        Usuario.Senha = BCrypt.Net.BCrypt.HashPassword(Senha.Nova);
        await Db.SaveChangesAsync();
        await AuditarAsync("Alterou senha");
        TempData["Success"] = "Senha alterada com sucesso.";
        return RedirectToPage();
    }
}
