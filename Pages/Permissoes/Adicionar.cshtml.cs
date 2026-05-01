using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Permissoes;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty, Required] public string Nome { get; set; } = string.Empty;
    [BindProperty] public Dictionary<string, bool> Perms { get; set; } = [];

    public static readonly (string Label, string[] Keys)[] Modulos =
    [
        ("Clientes",         ["vCliente","aCliente","eCliente","dCliente"]),
        ("Produtos",         ["vProduto","aProduto","eProduto","dProduto"]),
        ("Serviços",         ["vServico","aServico","eServico","dServico"]),
        ("Ordens de Serviço",["vOs","aOs","eOs","dOs"]),
        ("Vendas",           ["vVenda","aVenda","eVenda","dVenda"]),
        ("Garantias",        ["vGarantia","aGarantia","eGarantia","dGarantia"]),
        ("Arquivos",         ["vArquivo","aArquivo","eArquivo","dArquivo"]),
        ("Lançamentos",      ["vLancamento","aLancamento","eLancamento","dLancamento"]),
        ("Cobranças",        ["vCobranca","aCobranca","eCobranca","dCobranca"]),
        ("Configurações",    ["cUsuario","cPermissao","cEmitente","cSistema","cBackup","cAuditoria","cEmail"]),
        ("Relatórios",       ["rCliente","rProduto","rServico","rOs","rVenda","rFinanceiro"]),
    ];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cPermissao")) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("cPermissao")) return SemPermissao();
        if (!ModelState.IsValid) { await SetLayoutDataAsync(); return Page(); }

        var dict = Perms.Where(kv => kv.Value).ToDictionary(kv => kv.Key, _ => 1);
        Db.Permissoes.Add(new Permissao
        {
            Nome       = Nome,
            Permissoes = JsonSerializer.Serialize(dict),
            Situacao   = true,
            Data       = DateOnly.FromDateTime(DateTime.Today)
        });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Permissão criada.";
        return RedirectToPage("/Permissoes/Index");
    }
}
