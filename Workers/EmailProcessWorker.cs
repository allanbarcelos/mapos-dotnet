using mapos_dotnet.Services;

namespace mapos_dotnet.Workers;

/// <summary>
/// Envia e-mails pendentes no intervalo configurado em Configurações → E-mail (padrão: 2 min).
/// </summary>
public class EmailProcessWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailProcessWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Default = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("EmailProcessWorker iniciado");

        while (!ct.IsCancellationRequested)
        {
            var interval = Default;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguracaoService>();
                var configs = await config.GetAllAsync();
                if (int.TryParse(configs.GetValueOrDefault("email_process_interval", "2"), out var min) && min > 0)
                    interval = TimeSpan.FromMinutes(min);

                var svc = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
                await svc.ProcessAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no EmailProcessWorker");
            }

            await Task.Delay(interval, ct).ContinueWith(_ => { }, CancellationToken.None);
        }
    }
}
