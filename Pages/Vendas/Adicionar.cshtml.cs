using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Vendas;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Cliente> Clientes { get; set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Cliente é obrigatório")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "Data é obrigatória")]
        public DateOnly DataVenda { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public string Status { get; set; } = "Aberto";
        public string? Observacoes { get; set; }
        public string? ObservacoesCliente { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aVenda")) return SemPermissao();
        await SetLayoutDataAsync();
        Clientes = await Db.Clientes.OrderBy(c => c.NomeCliente).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aVenda")) return SemPermissao();
        if (!ModelState.IsValid)
        {
            Clientes = await Db.Clientes.OrderBy(c => c.NomeCliente).ToListAsync();
            await SetLayoutDataAsync();
            return Page();
        }

        var venda = new Venda
        {
            ClienteId          = Input.ClienteId,
            DataVenda          = Input.DataVenda,
            Status             = Input.Status,
            Observacoes        = Input.Observacoes,
            ObservacoesCliente = Input.ObservacoesCliente,
            UsuarioId          = UsuarioId,
            TipoDesconto       = "real"
        };

        Db.Vendas.Add(venda);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Criou Venda #{venda.Id}");
        TempData["Success"] = "Venda criada com sucesso.";
        return RedirectToPage("/Vendas/Visualizar", new { id = venda.Id });
    }
}
