using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Cobrancas;

public class VisualizarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IMercadoPagoService mpSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Cobranca Cobranca { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vCobranca")) return SemPermissao();
        await SetLayoutDataAsync();

        var cobranca = await Db.Cobrancas
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.Os)
            .Include(c => c.Venda)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cobranca is null)
        {
            TempData["Error"] = "Cobrança não encontrada.";
            return RedirectToPage("Index");
        }

        Cobranca = cobranca;
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
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostAtualizarStatusAsync(int id, string status)
    {
        if (!await TemPermissaoAsync("eCobranca")) return SemPermissao();

        var cobranca = await Db.Cobrancas.FindAsync(id);
        if (cobranca is not null)
        {
            cobranca.Status = status;
            await Db.SaveChangesAsync();
            await AuditarAsync($"Atualizou status da cobrança #{id} para {status}");
            TempData["Success"] = "Status atualizado.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEnviarEmailAsync(int id)
    {
        if (!await TemPermissaoAsync("vCobranca")) return SemPermissao();

        var cobranca = await Db.Cobrancas
            .Include(c => c.Cliente)
            .Include(c => c.Os)
            .Include(c => c.Venda)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cobranca is null) { TempData["Error"] = "Cobrança não encontrada."; return RedirectToPage(new { id }); }

        var email = cobranca.Cliente?.Email;
        if (string.IsNullOrWhiteSpace(email)) { TempData["Error"] = "Cliente sem e-mail cadastrado."; return RedirectToPage(new { id }); }

        var configs    = await ConfigSvc.GetAllAsync();
        var appName    = configs.GetValueOrDefault("app_name", "Map-OS");
        var referencia = cobranca.OsId.HasValue ? $"OS #{cobranca.OsId}" : $"Venda #{cobranca.VendaId}";
        var assunto    = $"Cobrança - {appName} - {referencia}";
        var corpo      = $"""
            <p>Olá, {cobranca.Cliente?.NomeCliente}.</p>
            <p>Segue sua cobrança referente a <strong>{referencia}</strong>.</p>
            <ul>
              <li><strong>Valor:</strong> {(cobranca.Total.HasValue ? cobranca.Total.Value.ToString("N2") : "-")}</li>
              <li><strong>Vencimento:</strong> {cobranca.ExpireAt:dd/MM/yyyy}</li>
              <li><strong>Status:</strong> {cobranca.Status}</li>
              {(cobranca.Link    is not null ? $"<li><strong>Link de pagamento:</strong> <a href='{cobranca.Link}'>{cobranca.Link}</a></li>" : "")}
              {(cobranca.Barcode is not null ? $"<li><strong>Código de barras:</strong> {cobranca.Barcode}</li>" : "")}
              {(cobranca.Pdf     is not null ? $"<li><strong>Boleto PDF:</strong> <a href='{cobranca.Pdf}'>Download</a></li>" : "")}
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
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAtualizarGatewayAsync(int id)
    {
        if (!await TemPermissaoAsync("eCobranca")) return SemPermissao();

        var (ok, erro) = await mpSvc.AtualizarDadosAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Dados atualizados do gateway." : erro;
        await AuditarAsync(ok ? $"Atualizou dados do gateway da cobrança #{id}" : $"Falha ao atualizar gateway da cobrança #{id}: {erro}");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelarGatewayAsync(int id)
    {
        if (!await TemPermissaoAsync("eCobranca")) return SemPermissao();

        var (ok, erro) = await mpSvc.CancelarAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Cobrança cancelada no gateway." : erro;
        await AuditarAsync(ok ? $"Cancelou cobrança #{id} no gateway" : $"Falha ao cancelar cobrança #{id}: {erro}");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConfirmarPagamentoAsync(int id)
    {
        if (!await TemPermissaoAsync("eCobranca")) return SemPermissao();

        var (ok, erro) = await mpSvc.ConfirmarPagamentoAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Pagamento confirmado/capturado." : erro;
        await AuditarAsync(ok ? $"Confirmou pagamento da cobrança #{id}" : $"Falha ao confirmar pagamento da cobrança #{id}: {erro}");
        return RedirectToPage(new { id });
    }
}
