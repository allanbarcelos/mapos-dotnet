using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Arquivos;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IList<Documento> Documentos { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vArquivo"))
            return SemPermissao();

        await SetLayoutDataAsync();
        PodeAdicionar = await TemPermissaoAsync("aArquivo");
        Documentos = await Db.Documentos.AsNoTracking().OrderByDescending(d => d.Id).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dArquivo"))
            return SemPermissao();

        var documento = await Db.Documentos.FindAsync(id);
        if (documento is not null)
        {
            // Remove physical file if it exists
            if (!string.IsNullOrEmpty(documento.Path) && System.IO.File.Exists(documento.Path))
                System.IO.File.Delete(documento.Path);

            Db.Documentos.Remove(documento);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Excluiu arquivo #{id} - {documento.Nome}");
            TempData["Success"] = "Arquivo excluído com sucesso.";
        }

        return RedirectToPage();
    }
}
