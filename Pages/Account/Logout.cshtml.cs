using System.Security.Claims;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace mapos_dotnet.Pages.Account;

public class LogoutModel(IAuditService auditSvc) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var nome = User.FindFirstValue(ClaimTypes.Name) ?? "Desconhecido";
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (int.TryParse(idStr, out var id))
            await auditSvc.LogAsync(nome, "Efetuou logout do sistema", ip, id);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }
}
