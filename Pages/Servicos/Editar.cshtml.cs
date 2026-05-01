using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Servicos;

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
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório.")]
        [Range(0, 9999999.99, ErrorMessage = "Informe um preço válido.")]
        public decimal Preco { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("eServico"))
            return SemPermissao();

        var servico = await Db.Servicos.FindAsync(Id);
        if (servico is null)
        {
            TempData["Error"] = "Serviço não encontrado.";
            return RedirectToPage("Index");
        }

        Input = new InputModel
        {
            Nome      = servico.Nome,
            Descricao = servico.Descricao,
            Preco     = servico.Preco
        };

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eServico"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var servico = await Db.Servicos.FindAsync(Id);
        if (servico is null)
        {
            TempData["Error"] = "Serviço não encontrado.";
            return RedirectToPage("Index");
        }

        servico.Nome      = Input.Nome;
        servico.Descricao = Input.Descricao;
        servico.Preco     = Input.Preco;

        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou serviço #{servico.Id} - {servico.Nome}");

        TempData["Success"] = "Serviço atualizado com sucesso.";
        return RedirectToPage("Index");
    }
}
