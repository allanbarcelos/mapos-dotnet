using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Servicos;

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
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório.")]
        [Range(0, 9999999.99, ErrorMessage = "Informe um preço válido.")]
        public decimal Preco { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aServico"))
            return SemPermissao();

        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aServico"))
            return SemPermissao();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var servico = new Servico
        {
            Nome      = Input.Nome,
            Descricao = Input.Descricao,
            Preco     = Input.Preco
        };

        Db.Servicos.Add(servico);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Adicionou serviço #{servico.Id} - {servico.Nome}");

        TempData["Success"] = "Serviço cadastrado com sucesso.";
        return RedirectToPage("Index");
    }
}
