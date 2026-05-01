using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Usuarios;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IReadOnlyList<Usuario> Usuarios { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cUsuario"))
            return SemPermissao();

        await SetLayoutDataAsync();
        PodeAdicionar = await TemPermissaoAsync("cUsuario");

        Usuarios = await Db.Usuarios
            .Include(u => u.Permissao)
            .OrderBy(u => u.Nome)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostToggleSituacaoAsync(int id)
    {
        if (!await TemPermissaoAsync("cUsuario"))
            return SemPermissao();

        var usuario = await Db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            TempData["Error"] = "Usuário não encontrado.";
            return RedirectToPage();
        }

        usuario.Situacao = !usuario.Situacao;
        await Db.SaveChangesAsync();

        var status = usuario.Situacao ? "ativou" : "desativou";
        await AuditarAsync($"Usuário #{UsuarioId} {status} o usuário #{id} - {usuario.Nome}");

        TempData["Success"] = $"Usuário \"{usuario.Nome}\" {(usuario.Situacao ? "ativado" : "desativado")} com sucesso.";
        return RedirectToPage();
    }
}
