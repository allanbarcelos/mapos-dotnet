using System.Security.Claims;
using mapos_dotnet.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace mapos_dotnet.Pages.Mine;

[Authorize(AuthenticationSchemes = "Mine")]
public abstract class MinePageModelBase(ApplicationDbContext db) : PageModel
{
    protected ApplicationDbContext Db { get; } = db;

    protected int ClienteId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public string ClienteNome =>
        User.FindFirstValue(ClaimTypes.Name) ?? "Cliente";
}
