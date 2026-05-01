using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using mapos_dotnet.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class LoginModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Senha { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true &&
            User.Identity.AuthenticationType == "Mine")
            return RedirectToPage("/Mine/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var cliente = await db.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == Input.Email && !string.IsNullOrEmpty(c.Senha));

        if (cliente is null || string.IsNullOrEmpty(cliente.Senha) ||
            !BCrypt.Net.BCrypt.Verify(Input.Senha, cliente.Senha))
        {
            ErrorMessage = "E-mail ou senha inválidos.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
            new(ClaimTypes.Name,           cliente.NomeCliente),
            new(ClaimTypes.Email,          cliente.Email!),
        };

        var identity  = new ClaimsIdentity(claims, "Mine");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Mine", principal, new AuthenticationProperties
        {
            IsPersistent    = true,
            ExpiresUtc      = DateTimeOffset.UtcNow.AddHours(24)
        });

        return RedirectToPage("/Mine/Index");
    }
}
