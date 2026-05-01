namespace mapos_dotnet.Services;

public interface IEmailQueueService
{
    /// <summary>Envia todos os e-mails com status "pending".</summary>
    Task ProcessAsync(CancellationToken ct = default);

    /// <summary>
    /// Recoloca na fila e-mails com status "failed" ou travados em "sending"
    /// há mais de 5 minutos (equivalente ao email/retry do Map-OS PHP).
    /// </summary>
    Task RetryAsync(CancellationToken ct = default);
}
