using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Permissoes;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public int PermId { get; set; }
    [BindProperty, Required] public string Nome { get; set; } = string.Empty;
    [BindProperty] public Dictionary<string, bool> Perms { get; set; } = [];
    public Dictionary<string, bool> PermissoesAtuais { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("cPermissao")) return SemPermissao();
        var p = await Db.Permissoes.FindAsync(id);
        if (p is null) return NotFound();
        PermId = id;
        Nome = p.Nome;
        if (p.Permissoes is not null)
        {
            try
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(p.Permissoes);
                if (raw is not null)
                    PermissoesAtuais = raw.ToDictionary(kv => kv.Key, kv => kv.Value.ValueKind == JsonValueKind.Number ? kv.Value.GetInt32() == 1 : false);
            }
            catch { }
        }
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("cPermissao")) return SemPermissao();
        if (!ModelState.IsValid) { await SetLayoutDataAsync(); return Page(); }
        var p = await Db.Permissoes.FindAsync(PermId);
        if (p is null) return NotFound();
        var dict = Perms.Where(kv => kv.Value).ToDictionary(kv => kv.Key, _ => 1);
        p.Nome = Nome;
        p.Permissoes = JsonSerializer.Serialize(dict);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Permissão atualizada.";
        return RedirectToPage("/Permissoes/Index");
    }
}
