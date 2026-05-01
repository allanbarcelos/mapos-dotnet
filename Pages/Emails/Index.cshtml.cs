using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Emails;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<EmailQueue> Items { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages  { get; set; }
    public string? FiltroStatus { get; set; }

    public async Task<IActionResult> OnGetAsync(string? status, int p = 1)
    {
        if (!await TemPermissaoAsync("cEmail")) return SemPermissao();
        await SetLayoutDataAsync();

        FiltroStatus = status;
        var q = Db.EmailQueue.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(e => e.Status == status);

        int total = await q.CountAsync();
        int perPage = 20;
        TotalPages  = (int)Math.Ceiling(total / (double)perPage);
        CurrentPage = Math.Clamp(p, 1, Math.Max(1, TotalPages));

        Items = await q.OrderByDescending(e => e.Id)
            .Skip((CurrentPage - 1) * perPage).Take(perPage).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("cEmail")) return SemPermissao();
        var e = await Db.EmailQueue.FindAsync(id);
        if (e is not null) { Db.EmailQueue.Remove(e); await Db.SaveChangesAsync(); }
        TempData["Success"] = "E-mail removido da fila.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLimparAsync()
    {
        if (!await TemPermissaoAsync("cEmail")) return SemPermissao();
        var sent = await Db.EmailQueue.Where(e => e.Status == "sent").ToListAsync();
        Db.EmailQueue.RemoveRange(sent);
        await Db.SaveChangesAsync();
        TempData["Success"] = $"{sent.Count} e-mails enviados removidos da fila.";
        return RedirectToPage();
    }
}
