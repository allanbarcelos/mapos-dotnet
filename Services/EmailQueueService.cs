using System.Net;
using System.Net.Mail;
using System.Text.Json;
using mapos_dotnet.Data;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Services;

public class EmailQueueService(
    ApplicationDbContext db,
    IConfiguration config,
    ILogger<EmailQueueService> logger) : IEmailQueueService
{
    // E-mails travados em "sending" há mais de este tempo são resetados para "pending"
    private static readonly TimeSpan SendingExpiration = TimeSpan.FromMinutes(5);

    public async Task ProcessAsync(CancellationToken ct = default)
    {
        var pending = await db.EmailQueue
            .Where(e => e.Status == "pending")
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        // Marca todos como "sending" antes de tentar enviar
        foreach (var e in pending) { e.Status = "sending"; e.Date = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);

        using var smtp = BuildSmtpClient();

        foreach (var item in pending)
        {
            try
            {
                using var msg = BuildMailMessage(item);
                await smtp.SendMailAsync(msg, ct);
                item.Status = "sent";
                logger.LogInformation("E-mail {Id} enviado para {To}", item.Id, item.To);
            }
            catch (Exception ex)
            {
                item.Status = "failed";
                logger.LogError(ex, "Falha ao enviar e-mail {Id} para {To}", item.Id, item.To);
            }

            item.Date = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RetryAsync(CancellationToken ct = default)
    {
        var expiracao = DateTime.UtcNow - SendingExpiration;

        var requeue = await db.EmailQueue
            .Where(e => e.Status == "failed" ||
                        (e.Status == "sending" && e.Date < expiracao))
            .ToListAsync(ct);

        if (requeue.Count == 0) return;

        foreach (var e in requeue) e.Status = "pending";
        await db.SaveChangesAsync(ct);

        logger.LogInformation("{Count} e-mail(s) recolocados na fila para reenvio", requeue.Count);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SmtpClient BuildSmtpClient()
    {
        var host     = config["Smtp:Host"]     ?? "localhost";
        var port     = int.Parse(config["Smtp:Port"] ?? "587");
        var ssl      = bool.Parse(config["Smtp:EnableSsl"] ?? "true");
        var user     = config["Smtp:Username"] ?? string.Empty;
        var password = config["Smtp:Password"] ?? string.Empty;

        var client = new SmtpClient(host, port)
        {
            EnableSsl = ssl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrEmpty(user))
            client.Credentials = new NetworkCredential(user, password);

        return client;
    }

    private MailMessage BuildMailMessage(Models.EmailQueue item)
    {
        // "From" e "Subject" podem vir da coluna headers (JSON) ou de campos diretos
        string from    = config["Smtp:From"] ?? config["Smtp:Username"] ?? "noreply@mapos.local";
        string subject = item.Subject ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(item.Headers))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(item.Headers);
                if (headers is not null)
                {
                    if (headers.TryGetValue("From",    out var f) && !string.IsNullOrEmpty(f)) from    = f;
                    if (headers.TryGetValue("Subject", out var s) && !string.IsNullOrEmpty(s)) subject = s;
                }
            }
            catch { /* headers malformados — usa defaults */ }
        }

        var msg = new MailMessage
        {
            From       = new MailAddress(from),
            Subject    = subject,
            Body       = item.Message ?? string.Empty,
            IsBodyHtml = true,
        };

        foreach (var addr in item.To.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            msg.To.Add(addr);

        if (!string.IsNullOrWhiteSpace(item.Cc))
            foreach (var addr in item.Cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                msg.CC.Add(addr);

        if (!string.IsNullOrWhiteSpace(item.Bcc))
            foreach (var addr in item.Bcc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                msg.Bcc.Add(addr);

        return msg;
    }
}
