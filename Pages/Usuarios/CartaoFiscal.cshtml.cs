using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Usuarios;

public class CartaoFiscalModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IFiscalCodigoProtector fiscalProtector)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Usuario Usuario { get; set; } = null!;
    public string AppName { get; set; } = "Map-OS";
    /// <summary>Código descriptografado para exibição/impressão no cartão.</summary>
    public string CodigoParaExibir { get; set; } = string.Empty;
    /// <summary>True quando o código ainda está em texto plano (registro anterior à criptografia).</summary>
    public bool CodigoLegado { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("cSistema") && !await TemPermissaoAsync("cUsuario"))
            return SemPermissao();

        var usuario = await Db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.FiscalPdv);

        if (usuario is null || string.IsNullOrEmpty(usuario.FiscalPdvCodigo))
        {
            TempData["Error"] = "Usuário não possui credencial fiscal PDV.";
            return RedirectToPage("/Usuarios/Index");
        }

        Usuario = usuario;

        // Descriptografar o código para exibição no cartão
        if (fiscalProtector.EstaProtegido(usuario.FiscalPdvCodigo))
        {
            CodigoParaExibir = fiscalProtector.Desproteger(usuario.FiscalPdvCodigo);
        }
        else
        {
            // Código gerado antes da criptografia — exibir como está e avisar
            CodigoParaExibir = usuario.FiscalPdvCodigo;
            CodigoLegado     = true;
        }

        var configs = await ConfigSvc.GetAllAsync();
        AppName = configs.GetValueOrDefault("app_name", "Map-OS");
        return Page();
    }
}
