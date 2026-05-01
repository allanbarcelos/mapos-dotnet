using Microsoft.AspNetCore.DataProtection;

namespace mapos_dotnet.Services;

public interface IFiscalCodigoProtector
{
    string Proteger(string codigo);
    string Desproteger(string codigoProtegido);
    bool Verificar(string codigoInformado, string codigoProtegido);
    bool EstaProtegido(string valor);
}

/// <summary>
/// Protege FiscalPdvCodigo via AES (IDataProtection do ASP.NET Core).
/// Criptografia simétrica: permite exibir o código no cartão e verificar no PDV,
/// sem armazená-lo em texto plano no banco de dados.
/// </summary>
public class FiscalCodigoProtector : IFiscalCodigoProtector
{
    private readonly IDataProtector _protector;

    public FiscalCodigoProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FiscalPdvCodigo.v1");
    }

    public string Proteger(string codigo)
        => _protector.Protect(codigo);

    public string Desproteger(string codigoProtegido)
        => _protector.Unprotect(codigoProtegido);

    public bool Verificar(string codigoInformado, string codigoProtegido)
    {
        try { return Desproteger(codigoProtegido) == codigoInformado; }
        catch { return false; }
    }

    /// <summary>
    /// Tenta descriptografar — se falhar, o valor ainda está em texto plano (registros legados).
    /// </summary>
    public bool EstaProtegido(string valor)
    {
        try { _protector.Unprotect(valor); return true; }
        catch { return false; }
    }
}
