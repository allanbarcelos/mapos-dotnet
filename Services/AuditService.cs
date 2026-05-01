using mapos_dotnet.Data;
using mapos_dotnet.Models;

namespace mapos_dotnet.Services;

public interface IAuditService
{
    Task LogAsync(string usuario, string tarefa, string? ip = null, int? usuarioId = null);
}

public class AuditService(ApplicationDbContext db) : IAuditService
{
    public async Task LogAsync(string usuario, string tarefa, string? ip = null, int? usuarioId = null)
    {
        var now = DateTime.Now;
        db.Logs.Add(new Log
        {
            Usuario   = usuario,
            Tarefa    = tarefa,
            Data      = DateOnly.FromDateTime(now),
            Hora      = TimeOnly.FromDateTime(now),
            Ip        = ip,
            UsuarioId = usuarioId
        });
        await db.SaveChangesAsync();
    }
}
