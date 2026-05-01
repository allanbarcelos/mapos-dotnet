using mapos_dotnet.Services;

namespace mapos_dotnet.Workers;

/// <summary>
/// Recoloca na fila e-mails com falha ou travados, no intervalo configurado em Configurações → E-mail (padrão: 5 min).
/// </summary>
public class EmailRetryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailRetryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Default = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("EmailRetryWorker iniciado");

        while (!ct.IsCancellationRequested)
        {
            var interval = Default;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguracaoService>();
                var configs = await config.GetAllAsync();
                if (int.TryParse(configs.GetValueOrDefault("email_retry_interval", "5"), out var min) && min > 0)
                    interval = TimeSpan.FromMinutes(min);

                var svc = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
                await svc.RetryAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no EmailRetryWorker");
            }

            await Task.Delay(interval, ct).ContinueWith(_ => { }, CancellationToken.None);
        }
    }
}
