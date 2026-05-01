using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Pdv.Terminais;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(80)]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        var terminal = await Db.PdvTerminais.FindAsync(id);
        if (terminal is null) return NotFound();
        Input = new InputModel { Id = terminal.Id, Nome = terminal.Nome, Descricao = terminal.Descricao };
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        if (!ModelState.IsValid) { await SetLayoutDataAsync(); return Page(); }

        var terminal = await Db.PdvTerminais.FindAsync(Input.Id);
        if (terminal is null) return NotFound();

        terminal.Nome      = Input.Nome;
        terminal.Descricao = string.IsNullOrWhiteSpace(Input.Descricao) ? null : Input.Descricao;
        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou terminal PDV #{terminal.Id} - {terminal.Nome}");
        TempData["Success"] = $"Terminal \"{terminal.Nome}\" atualizado.";
        return RedirectToPage("Index");
    }
}
