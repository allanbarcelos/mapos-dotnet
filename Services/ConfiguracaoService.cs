using mapos_dotnet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace mapos_dotnet.Services;

public interface IConfiguracaoService
{
    Task<string?> GetAsync(string chave);
    Task<Dictionary<string, string>> GetAllAsync();
    Task SetAsync(string chave, string valor);
}

public class ConfiguracaoService(ApplicationDbContext db, IMemoryCache cache) : IConfiguracaoService
{
    private const string CacheKey = "configuracoes_all";

    public async Task<string?> GetAsync(string chave)
    {
        var all = await GetAllAsync();
        return all.TryGetValue(chave, out var v) ? v : null;
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        if (cache.TryGetValue(CacheKey, out Dictionary<string, string>? cached) && cached is not null)
            return cached;

        var configs = await db.Configuracoes.AsNoTracking().ToListAsync();
        var dict = configs.ToDictionary(c => c.Config, c => c.Valor ?? string.Empty);

        cache.Set(CacheKey, dict, TimeSpan.FromMinutes(30));
        return dict;
    }

    public async Task SetAsync(string chave, string valor)
    {
        var config = await db.Configuracoes.FirstOrDefaultAsync(c => c.Config == chave);
        if (config is null)
            db.Configuracoes.Add(new Models.Configuracao { Config = chave, Valor = valor });
        else
            config.Valor = valor;

        await db.SaveChangesAsync();
        cache.Remove(CacheKey);
    }
}
