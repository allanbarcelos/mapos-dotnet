using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace mapos_dotnet.Pages.Os;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public IReadOnlyList<Models.Os> OsList { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public string? Q { get; private set; }
    public string? FiltroStatus { get; private set; }
    public string? De { get; private set; }
    public string? Ate { get; private set; }
    public List<string> StatusList { get; private set; } = [];
    public bool PodeAdicionar { get; private set; }
    public bool PodeExcluir { get; private set; }

    private const int PageSize = 10;

    public async Task<IActionResult> OnGetAsync(
        string? q, string? status, string? de, string? ate, int page = 1)
    {
        if (!await TemPermissaoAsync("vOs"))
            return SemPermissao();

        await SetLayoutDataAsync();

        PodeAdicionar = await TemPermissaoAsync("aOs");
        PodeExcluir   = await TemPermissaoAsync("dOs");
        Q             = q;
        FiltroStatus  = status;
        De            = de;
        Ate           = ate;
        CurrentPage   = Math.Max(1, page);

        var statusJson = await ConfigSvc.GetAsync("os_status_list");
        if (!string.IsNullOrWhiteSpace(statusJson))
        {
            try { StatusList = JsonSerializer.Deserialize<List<string>>(statusJson) ?? []; }
            catch { StatusList = []; }
        }

        if (StatusList.Count == 0)
            StatusList = ["Aberto", "Em Andamento", "Finalizado", "Faturado", "Cancelado"];

        var query = Db.Os
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var busca = q.Trim().ToLower();
            if (int.TryParse(busca, out var osId))
                query = query.Where(o => o.Id == osId);
            else
                query = query.Where(o => o.Cliente.NomeCliente.ToLower().Contains(busca));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(de) && DateOnly.TryParse(de, out var dataInicio))
            query = query.Where(o => o.DataInicial >= dataInicio);

        if (!string.IsNullOrWhiteSpace(ate) && DateOnly.TryParse(ate, out var dataFim))
            query = query.Where(o => o.DataInicial <= dataFim);

        var total = await query.CountAsync();
        TotalPages  = (int)Math.Ceiling(total / (double)PageSize);
        CurrentPage = Math.Min(CurrentPage, Math.Max(1, TotalPages));

        OsList = await query
            .OrderByDescending(o => o.Id)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dOs"))
            return SemPermissao();

        var os = await Db.Os.FindAsync(id);
        if (os is null)
        {
            TempData["Error"] = "OS não encontrada.";
            return RedirectToPage();
        }

        if (os.Faturado)
        {
            TempData["Error"] = "Não é possível excluir uma OS faturada.";
            return RedirectToPage();
        }

        Db.Os.Remove(os);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Excluiu Ordem de Serviço #{id}");

        TempData["Success"] = $"OS #{id} excluída com sucesso.";
        return RedirectToPage();
    }
}
