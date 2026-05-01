using Microsoft.Extensions.Caching.Memory;

namespace mapos_dotnet.Services;

public interface ILoginThrottleService
{
    bool EstaBloqueado(string chave);
    void RegistrarFalha(string chave);
    void RegistrarSucesso(string chave);
    TimeSpan? TempoBloqueio(string chave);
}

/// <summary>
/// Bloqueia tentativas de login após <see cref="MaxTentativas"/> falhas consecutivas
/// na mesma chave (IP ou e-mail), por <see cref="BloqueioMinutos"/> minutos.
/// </summary>
public class LoginThrottleService(IMemoryCache cache) : ILoginThrottleService
{
    private const int MaxTentativas   = 5;
    private const int BloqueioMinutos = 15;

    private static string ChaveFalhas(string chave)   => $"lth_f:{chave}";
    private static string ChaveBloqueio(string chave) => $"lth_b:{chave}";

    public bool EstaBloqueado(string chave)
        => cache.TryGetValue(ChaveBloqueio(chave), out _);

    public TimeSpan? TempoBloqueio(string chave)
    {
        if (cache.TryGetValue<DateTimeOffset>(ChaveBloqueio(chave), out var expira))
            return expira - DateTimeOffset.UtcNow;
        return null;
    }

    public void RegistrarFalha(string chave)
    {
        var keyFalhas = ChaveFalhas(chave);
        var janela    = TimeSpan.FromMinutes(BloqueioMinutos);

        var falhas = (cache.TryGetValue<int>(keyFalhas, out var f) ? f : 0) + 1;
        cache.Set(keyFalhas, falhas, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = janela
        });

        if (falhas >= MaxTentativas)
        {
            var expira = DateTimeOffset.UtcNow.Add(janela);
            cache.Set(ChaveBloqueio(chave), expira, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expira
            });
        }
    }

    public void RegistrarSucesso(string chave)
    {
        cache.Remove(ChaveFalhas(chave));
        cache.Remove(ChaveBloqueio(chave));
    }
}
