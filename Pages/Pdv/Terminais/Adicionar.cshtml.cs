using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Pdv.Terminais;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(80)]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        if (!ModelState.IsValid) { await SetLayoutDataAsync(); return Page(); }

        var terminal = new PdvTerminal
        {
            Nome      = Input.Nome,
            Descricao = string.IsNullOrWhiteSpace(Input.Descricao) ? null : Input.Descricao,
            Status    = "disponivel",
        };
        Db.PdvTerminais.Add(terminal);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Cadastrou terminal PDV #{terminal.Id} - {terminal.Nome}");
        TempData["Success"] = $"Terminal \"{terminal.Nome}\" cadastrado com sucesso.";
        return RedirectToPage("Index");
    }
}
