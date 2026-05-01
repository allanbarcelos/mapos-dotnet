using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Cobrancas;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<Cobranca> Items { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages  { get; set; }

    public async Task<IActionResult> OnGetAsync(int p = 1)
    {
        if (!await TemPermissaoAsync("vCobranca")) return SemPermissao();
        await SetLayoutDataAsync();

        var configs = await ConfigSvc.GetAllAsync();
        int perPage = int.TryParse(configs.GetValueOrDefault("per_page", "10"), out var pp) ? pp : 10;

        var q = Db.Cobrancas
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.Os)
            .Include(c => c.Venda)
            .OrderByDescending(c => c.Id);

        int total = await q.CountAsync();
        TotalPages  = (int)Math.Ceiling(total / (double)perPage);
        CurrentPage = Math.Clamp(p, 1, Math.Max(1, TotalPages));

        Items = await q.Skip((CurrentPage - 1) * perPage).Take(perPage).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (!await TemPermissaoAsync("dCobranca")) return SemPermissao();

        var cobranca = await Db.Cobrancas.FindAsync(id);
        if (cobranca is not null)
        {
            Db.Cobrancas.Remove(cobranca);
            await Db.SaveChangesAsync();
            await AuditarAsync($"Excluiu cobrança #{id}");
            TempData["Success"] = "Cobrança excluída.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEnviarEmailAsync(int id)
    {
        if (!await TemPermissaoAsync("vCobranca")) return SemPermissao();

        var cobranca = await Db.Cobrancas
            .Include(c => c.Cliente)
            .Include(c => c.Os)
            .Include(c => c.Venda)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cobranca is null) { TempData["Error"] = "Cobrança não encontrada."; return RedirectToPage(); }

        var email = cobranca.Cliente?.Email;
        if (string.IsNullOrWhiteSpace(email)) { TempData["Error"] = "Cliente sem e-mail cadastrado."; return RedirectToPage(); }

        var configs   = await ConfigSvc.GetAllAsync();
        var appName   = configs.GetValueOrDefault("app_name", "Map-OS");
        var referencia = cobranca.OsId.HasValue ? $"OS #{cobranca.OsId}" : $"Venda #{cobranca.VendaId}";
        var assunto   = $"Cobrança - {appName} - {referencia}";
        var corpo     = $"""
            <p>Olá, {cobranca.Cliente?.NomeCliente}.</p>
            <p>Segue sua cobrança referente a <strong>{referencia}</strong>.</p>
            <ul>
              <li><strong>Valor:</strong> {(cobranca.Total.HasValue ? cobranca.Total.Value.ToString("N2") : "-")}</li>
              <li><strong>Vencimento:</strong> {cobranca.ExpireAt:dd/MM/yyyy}</li>
              <li><strong>Status:</strong> {cobranca.Status}</li>
              {(cobranca.Link is not null ? $"<li><strong>Link de pagamento:</strong> <a href='{cobranca.Link}'>{cobranca.Link}</a></li>" : "")}
              {(cobranca.Barcode is not null ? $"<li><strong>Código de barras:</strong> {cobranca.Barcode}</li>" : "")}
              {(cobranca.Pdf is not null ? $"<li><strong>Boleto PDF:</strong> <a href='{cobranca.Pdf}'>Download</a></li>" : "")}
            </ul>
            """;

        Db.EmailQueue.Add(new EmailQueue
        {
            To      = email,
            Subject = assunto,
            Message = corpo,
            Status  = "pending",
            Date    = DateTime.UtcNow,
            Headers = System.Text.Json.JsonSerializer.Serialize(new { From = email, Subject = assunto }),
        });
        await Db.SaveChangesAsync();
        await AuditarAsync($"Enfileirou e-mail de cobrança #{id}");
        TempData["Success"] = "E-mail adicionado na fila.";
        return RedirectToPage();
    }
}
