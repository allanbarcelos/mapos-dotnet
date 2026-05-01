using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Pesquisar;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public string? Termo { get; set; }
    public List<Cliente>            Clientes   { get; set; } = [];
    public List<Models.Os>          OsList     { get; set; } = [];
    public List<Venda>              Vendas     { get; set; } = [];
    public List<Produto>            Produtos   { get; set; } = [];
    public int Total => Clientes.Count + OsList.Count + Vendas.Count + Produtos.Count;

    public async Task<IActionResult> OnGetAsync(string? termo)
    {
        await SetLayoutDataAsync();
        if (string.IsNullOrWhiteSpace(termo)) return Page();

        Termo = termo.Trim();
        var t = Termo.ToLower();

        if (await TemPermissaoAsync("vCliente"))
            Clientes = await Db.Clientes.AsNoTracking()
                .Where(c => c.NomeCliente.ToLower().Contains(t)
                         || (c.Documento != null && c.Documento.Contains(t))
                         || (c.Email     != null && c.Email.ToLower().Contains(t))
                         || (c.Telefone  != null && c.Telefone.Contains(t)))
                .OrderBy(c => c.NomeCliente).Take(20).ToListAsync();

        if (await TemPermissaoAsync("vOs"))
            OsList = await Db.Os.AsNoTracking()
                .Include(o => o.Cliente)
                .Where(o => o.Cliente.NomeCliente.ToLower().Contains(t)
                         || (o.Defeito         != null && o.Defeito.ToLower().Contains(t))
                         || (o.DescricaoProduto != null && o.DescricaoProduto.ToLower().Contains(t)))
                .OrderByDescending(o => o.Id).Take(20).ToListAsync();

        if (await TemPermissaoAsync("vVenda"))
            Vendas = await Db.Vendas.AsNoTracking()
                .Include(v => v.Cliente)
                .Where(v => v.Cliente.NomeCliente.ToLower().Contains(t)
                         || (v.Observacoes != null && v.Observacoes.ToLower().Contains(t)))
                .OrderByDescending(v => v.Id).Take(20).ToListAsync();

        if (await TemPermissaoAsync("vProduto"))
            Produtos = await Db.Produtos.AsNoTracking()
                .Where(p => p.Descricao.ToLower().Contains(t)
                         || (p.CodDeBarra != null && p.CodDeBarra.Contains(t)))
                .OrderBy(p => p.Descricao).Take(20).ToListAsync();

        return Page();
    }
}
