using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using mapos_dotnet.Data;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Account;

public class LoginModel(
    ApplicationDbContext db,
    IAuditService auditSvc,
    ILoginThrottleService throttle) : PageModel
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

        public bool LembrarMe { get; set; }

        public string Destino { get; set; } = "admin";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var ip    = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var email = Input.Email.Trim().ToLowerInvariant();

        // Verificar bloqueio por IP e por e-mail antes de tocar no banco
        if (throttle.EstaBloqueado(ip) || throttle.EstaBloqueado(email))
        {
            var espera  = throttle.TempoBloqueio(ip) ?? throttle.TempoBloqueio(email);
            var minutos = (int)Math.Ceiling(espera?.TotalMinutes ?? 15);
            ErrorMessage = $"Muitas tentativas incorretas. Tente novamente em {minutos} minuto(s).";
            return Page();
        }

        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == Input.Email && u.Situacao);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(Input.Senha, usuario.Senha))
        {
            throttle.RegistrarFalha(ip);
            throttle.RegistrarFalha(email);
            ErrorMessage = "E-mail ou senha inválidos.";
            return Page();
        }

        if (usuario.DataExpiracao.HasValue && usuario.DataExpiracao.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            ErrorMessage = "Conta expirada. Entre em contato com o administrador.";
            return Page();
        }

        // Login bem-sucedido — resetar contadores de bloqueio
        throttle.RegistrarSucesso(ip);
        throttle.RegistrarSucesso(email);

        var sessaoTipo = Input.Destino == "pdv" ? "pdv" : "admin";

        // Nota: permissao_id NÃO é incluído no cookie.
        // O PageModelBase resolve o PermissaoId do banco usando UsuarioId,
        // evitando que o usuário manipule o cookie para elevar permissões.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name,           usuario.Nome),
            new(ClaimTypes.Email,          usuario.Email),
            new("url_image",               usuario.UrlImageUser ?? string.Empty),
            new("sessao_tipo",             sessaoTipo),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProps = new AuthenticationProperties
        {
            IsPersistent = Input.LembrarMe,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(Input.LembrarMe ? 168 : 2)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        var destino = Input.Destino == "pdv" ? "PDV" : "área administrativa";
        await auditSvc.LogAsync(usuario.Nome, $"Efetuou login no sistema ({destino})",
            HttpContext.Connection.RemoteIpAddress?.ToString(), usuario.Id);

        return Input.Destino == "pdv"
            ? RedirectToPage("/Pdv/AbrirCaixa")
            : RedirectToPage("/Index");
    }
}
