using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Vendas;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public List<Cliente> Clientes { get; set; } = [];

    public class InputModel
    {
        public int Id { get; set; }
        [Required] public int ClienteId { get; set; }
        [Required] public DateOnly DataVenda { get; set; }
        public string Status { get; set; } = "Aberto";
        public string? Observacoes { get; set; }
        public string? ObservacoesCliente { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("eVenda")) return SemPermissao();
        var v = await Db.Vendas.FindAsync(id);
        if (v is null) return NotFound();
        var ctrlEditVendas = await ConfigSvc.GetAsync("control_edit_vendas");
        if (v.Faturado && ctrlEditVendas != "1")
        {
            TempData["Error"] = "Não é permitido editar vendas faturadas.";
            return RedirectToPage("/Vendas/Index");
        }
        Input = new InputModel { Id = id, ClienteId = v.ClienteId, DataVenda = v.DataVenda, Status = v.Status, Observacoes = v.Observacoes, ObservacoesCliente = v.ObservacoesCliente };
        Clientes = await Db.Clientes.OrderBy(c => c.NomeCliente).ToListAsync();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eVenda")) return SemPermissao();
        if (!ModelState.IsValid)
        {
            Clientes = await Db.Clientes.OrderBy(c => c.NomeCliente).ToListAsync();
            await SetLayoutDataAsync();
            return Page();
        }
        var v = await Db.Vendas.FindAsync(Input.Id);
        if (v is null) return NotFound();
        v.ClienteId = Input.ClienteId;
        v.DataVenda = Input.DataVenda;
        v.Status = Input.Status;
        v.Observacoes = Input.Observacoes;
        v.ObservacoesCliente = Input.ObservacoesCliente;
        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou Venda #{Input.Id}");
        TempData["Success"] = "Venda atualizada.";
        return RedirectToPage("/Vendas/Visualizar", new { id = Input.Id });
    }
}
