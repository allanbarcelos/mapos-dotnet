using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using mapos_dotnet.Data;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    public bool TrocaObrigatoria { get; set; }

    public class SenhaInput
    {
        [Required] public string Atual { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Mínimo de 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "A senha deve conter ao menos 1 letra maiúscula e 1 número.")]
        public string Nova { get; set; } = string.Empty;

        [Compare(nameof(Nova), ErrorMessage = "As senhas não conferem.")]
        public string Confirmar { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await SetLayoutDataAsync();
        Usuario = await Db.Usuarios.FindAsync(UsuarioId);
        TrocaObrigatoria = User.FindFirstValue("deve_trocar_senha") == "true";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await SetLayoutDataAsync();
        Usuario = await Db.Usuarios.FindAsync(UsuarioId);
        if (Usuario is null) return NotFound();
        TrocaObrigatoria = User.FindFirstValue("deve_trocar_senha") == "true";
        if (!ModelState.IsValid) return Page();

        if (!BCrypt.Net.BCrypt.Verify(Senha.Atual, Usuario.Senha))
        {
            ModelState.AddModelError(nameof(Senha.Atual), "Senha atual incorreta.");
            return Page();
        }

        Usuario.Senha          = BCrypt.Net.BCrypt.HashPassword(Senha.Nova);
        Usuario.SenhaAlteradaEm = DateTime.UtcNow;
        Usuario.PrimeiroAcesso  = false;
        await Db.SaveChangesAsync();
        await AuditarAsync("Alterou senha");

        // Re-assina o cookie removendo o claim de troca obrigatória
        if (TrocaObrigatoria)
        {
            var claims = User.Claims
                .Where(c => c.Type != "deve_trocar_senha")
                .ToList();
            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = false });
        }

        TempData["Success"] = "Senha alterada com sucesso.";
        return RedirectToPage();
    }
}
