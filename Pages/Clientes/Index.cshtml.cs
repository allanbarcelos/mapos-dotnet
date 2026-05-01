using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Clientes;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IReadOnlyList<Cliente> Clientes { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public string? Q { get; private set; }
    public bool FiltroFornecedor { get; private set; }
    public bool PodeAdicionar { get; private set; }

    private const int PageSize = 10;

    public async Task<IActionResult> OnGetAsync(string? q, bool fornecedor = false, int page = 1)
    {
        if (!await TemPermissaoAsync("vCliente"))
            return SemPermissao();

        await SetLayoutDataAsync();

        PodeAdicionar = await TemPermissaoAsync("aCliente");
        Q = q;
        FiltroFornecedor = fornecedor;
        CurrentPage = Math.Max(1, page);

        var query = Db.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var busca = q.Trim().ToLower();
            query = query.Where(c =>
                c.NomeCliente.ToLower().Contains(busca) ||
                (c.Documento != null && c.Documento.Contains(busca)));
        }

        if (fornecedor)
            query = query.Where(c => c.Fornecedor);

        var total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        CurrentPage = Math.Min(CurrentPage, Math.Max(1, TotalPages));

        Clientes = await query
            .OrderBy(c => c.NomeCliente)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dCliente"))
            return SemPermissao();

        var cliente = await Db.Clientes.FindAsync(id);
        if (cliente is null)
        {
            TempData["Error"] = "Cliente não encontrado.";
            return RedirectToPage();
        }

        Db.Clientes.Remove(cliente);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Excluiu cliente #{id} - {cliente.NomeCliente}");

        TempData["Success"] = $"Cliente \"{cliente.NomeCliente}\" excluído com sucesso.";
        return RedirectToPage();
    }
}
