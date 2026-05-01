using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Contas;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

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
        if (!await TemPermissaoAsync("eConta"))
            return SemPermissao();

        var conta = await Db.Contas.FindAsync(Id);
        if (conta is null)
        {
            TempData["Error"] = "Conta não encontrada.";
            return RedirectToPage("Index");
        }

        Input = new InputModel
        {
            Nome          = conta.Nome,
            Banco         = conta.Banco,
            NumeroAgencia = conta.NumeroAgencia,
            Tipo          = conta.Tipo,
            Saldo         = conta.Saldo,
            Status        = conta.Status
        };

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eConta"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var conta = await Db.Contas.FindAsync(Id);
        if (conta is null)
        {
            TempData["Error"] = "Conta não encontrada.";
            return RedirectToPage("Index");
        }

        conta.Nome          = Input.Nome;
        conta.Banco         = Input.Banco;
        conta.NumeroAgencia = Input.NumeroAgencia;
        conta.Tipo          = Input.Tipo;
        conta.Saldo         = Input.Saldo;
        conta.Status        = Input.Status;

        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou conta #{conta.Id} - {conta.Nome}");

        TempData["Success"] = "Conta atualizada com sucesso.";
        return RedirectToPage("Index");
    }
}
