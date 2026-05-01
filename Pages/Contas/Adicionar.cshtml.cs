using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Contas;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(45)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(45)]
        public string? Banco { get; set; }

        [MaxLength(45)]
        public string? NumeroAgencia { get; set; }

        [MaxLength(80)]
        public string? Tipo { get; set; }

        public decimal Saldo { get; set; }

        public bool Status { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aConta"))
            return SemPermissao();

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aConta"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var conta = new Conta
        {
            Nome           = Input.Nome,
            Banco          = Input.Banco,
            NumeroAgencia  = Input.NumeroAgencia,
            Tipo           = Input.Tipo,
            Saldo          = Input.Saldo,
            Status         = Input.Status,
            Cadastro       = DateOnly.FromDateTime(DateTime.Today)
        };

        Db.Contas.Add(conta);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Adicionou conta #{conta.Id} - {conta.Nome}");

        TempData["Success"] = "Conta cadastrada com sucesso.";
        return RedirectToPage("Index");
    }
}
