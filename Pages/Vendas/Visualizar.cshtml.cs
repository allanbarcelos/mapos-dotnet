using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Vendas;

public class VisualizarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IMercadoPagoService mpSvc,
    IConfiguration configuration)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Venda Venda { get; set; } = null!;
    public List<Produto> Produtos { get; set; } = [];
    public bool MpConfigurado { get; set; }

    [BindProperty] public int AddProdutoId     { get; set; }
    [BindProperty] public int AddQuantidade    { get; set; } = 1;
    [BindProperty] public decimal AddPreco     { get; set; }
    [BindProperty] public decimal DescontoValor { get; set; }
    [BindProperty] public string TipoDesconto  { get; set; } = "real";

    private async Task<Venda?> CarregarAsync(int id) =>
        await Db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Cobrancas)
            .FirstOrDefaultAsync(v => v.Id == id);

    private void RecalcularTotal(Venda v)
    {
        var subtotal = v.Itens.Sum(i => i.SubTotal);
        v.ValorDesconto = TipoDesconto == "percent"
            ? Math.Round(subtotal * v.Desconto / 100, 2)
            : v.Desconto;
        v.ValorTotal = subtotal - v.ValorDesconto;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vVenda")) return SemPermissao();
        await SetLayoutDataAsync();
        var v = await CarregarAsync(id);
        if (v is null) return NotFound();
        Venda = v;
        Produtos = await Db.Produtos.OrderBy(p => p.Descricao).ToListAsync();
        MpConfigurado = !string.IsNullOrWhiteSpace(configuration["MercadoPago:AccessToken"]);
        return Page();
    }

    public async Task<IActionResult> OnPostAdicionarProdutoAsync(int id)
    {
        if (!await TemPermissaoAsync("eVenda")) return SemPermissao();
        var venda = await CarregarAsync(id);
        if (venda is null) return NotFound();

        var produto = await Db.Produtos.FindAsync(AddProdutoId);
        if (produto is null) return RedirectToPage(new { id });

        var item = new ItemVenda
        {
            VendaId    = id,
            ProdutoId  = AddProdutoId,
            Quantidade = AddQuantidade,
            Preco      = AddPreco > 0 ? AddPreco : produto.PrecoVenda,
        };
        item.SubTotal = item.Quantidade * item.Preco;
        Db.ItensVenda.Add(item);
        venda.Itens.Add(item);
        RecalcularTotal(venda);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostExcluirProdutoAsync(int id, int itemId)
    {
        if (!await TemPermissaoAsync("eVenda")) return SemPermissao();
        var item = await Db.ItensVenda.FindAsync(itemId);
        if (item is not null) Db.ItensVenda.Remove(item);
        var venda = await CarregarAsync(id);
        if (venda is not null) RecalcularTotal(venda);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAdicionarDescontoAsync(int id)
    {
        if (!await TemPermissaoAsync("eVenda")) return SemPermissao();
        var venda = await CarregarAsync(id);
        if (venda is null) return NotFound();
        venda.Desconto     = DescontoValor;
        venda.TipoDesconto = TipoDesconto;
        RecalcularTotal(venda);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostGerarBoletoAsync(int id)
    {
        if (!await TemPermissaoAsync("aCobranca")) return SemPermissao();

        var (ok, erro) = await mpSvc.GerarBoletoAsync(id, "venda");
        TempData[ok ? "Success" : "Error"] = ok ? "Boleto gerado com sucesso." : erro;
        if (ok) await AuditarAsync($"Gerou boleto MercadoPago para Venda #{id}");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostFaturarAsync(int id)
    {
        if (!await TemPermissaoAsync("aLancamento")) return SemPermissao();
        var venda = await CarregarAsync(id);
        if (venda is null) return NotFound();
        if (venda.Faturado) return RedirectToPage(new { id });

        var cliente = await Db.Clientes.FindAsync(venda.ClienteId);
        var lancamento = new Lancamento
        {
            Descricao         = $"Venda #{venda.Id}",
            Valor             = venda.ValorTotal,
            ValorDesconto     = venda.ValorTotal,
            DataVencimento    = DateOnly.FromDateTime(DateTime.Today),
            Tipo              = "receita",
            ClienteFornecedor = cliente?.NomeCliente,
            ClienteId         = venda.ClienteId,
            UsuarioId         = UsuarioId,
        };
        Db.Lancamentos.Add(lancamento);
        await Db.SaveChangesAsync();

        venda.Faturado    = true;
        venda.LancamentoId = lancamento.Id;
        await Db.SaveChangesAsync();
        await AuditarAsync($"Faturou Venda #{id}");
        TempData["Success"] = "Venda faturada com sucesso.";
        return RedirectToPage(new { id });
    }
}
