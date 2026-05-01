using System.Text.Json;
using mapos_dotnet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace mapos_dotnet.Services;

public interface IPermissaoService
{
    Task<bool> TemPermissaoAsync(int permissaoId, string atividade);
    Task<Dictionary<string, bool>> GetPermissoesAsync(int permissaoId);
    /// <summary>Retorna o PermissaoId do usuário consultando o banco (com cache de 5 min).</summary>
    Task<int> GetPermissaoIdByUsuarioIdAsync(int usuarioId);
    void InvalidarCache(int permissaoId);
    void InvalidarCachePorUsuario(int usuarioId);
}

public class PermissaoService(ApplicationDbContext db, IMemoryCache cache) : IPermissaoService
{
    public async Task<bool> TemPermissaoAsync(int permissaoId, string atividade)
    {
        var perms = await GetPermissoesAsync(permissaoId);
        return perms.TryGetValue(atividade, out var value) && value;
    }

    public async Task<Dictionary<string, bool>> GetPermissoesAsync(int permissaoId)
    {
        var cacheKey = $"permissao_{permissaoId}";
        if (cache.TryGetValue(cacheKey, out Dictionary<string, bool>? cached) && cached is not null)
            return cached;

        var permissao = await db.Permissoes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissaoId);

        if (permissao?.Permissoes is null)
            return [];

        var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(permissao.Permissoes);
            if (raw is not null)
            {
                foreach (var (key, val) in raw)
                {
                    dict[key] = val.ValueKind switch
                    {
                        JsonValueKind.True   => true,
                        JsonValueKind.False  => false,
                        JsonValueKind.Number => val.GetInt32() == 1,
                        JsonValueKind.String => val.GetString() == "1",
                        _ => false
                    };
                }
            }
        }
        catch { /* retorna dict vazio se JSON inválido */ }

        cache.Set(cacheKey, dict, TimeSpan.FromMinutes(10));
        return dict;
    }

    public async Task<int> GetPermissaoIdByUsuarioIdAsync(int usuarioId)
    {
        var cacheKey = $"usuario_permissao_id:{usuarioId}";
        if (cache.TryGetValue(cacheKey, out int permissaoId))
            return permissaoId;

        permissaoId = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => u.PermissaoId)
            .FirstOrDefaultAsync();

        cache.Set(cacheKey, permissaoId, TimeSpan.FromMinutes(5));
        return permissaoId;
    }

    public void InvalidarCache(int permissaoId)
        => cache.Remove($"permissao_{permissaoId}");

    public void InvalidarCachePorUsuario(int usuarioId)
        => cache.Remove($"usuario_permissao_id:{usuarioId}");
}
