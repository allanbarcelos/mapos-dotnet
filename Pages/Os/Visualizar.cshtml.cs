using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Os;

public class VisualizarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IMercadoPagoService mpSvc,
    IConfiguration configuration)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public Models.Os Os { get; set; } = null!;
    public List<Produto> Produtos { get; set; } = [];
    public List<Servico> Servicos { get; set; } = [];
    public bool MpConfigurado { get; set; }

    [BindProperty] public int    AddProdutoId    { get; set; }
    [BindProperty] public int    AddProdQtd      { get; set; } = 1;
    [BindProperty] public decimal AddProdPreco   { get; set; }
    [BindProperty] public int    AddServicoId    { get; set; }
    [BindProperty] public double AddServQtd      { get; set; } = 1;
    [BindProperty] public decimal AddServPreco   { get; set; }
    [BindProperty] public string  Anotacao       { get; set; } = string.Empty;
    [BindProperty] public decimal DescontoValor  { get; set; }
    [BindProperty] public string  TipoDesconto   { get; set; } = "real";

    private async Task<Models.Os?> CarregarAsync(int id) =>
        await Db.Os
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .Include(o => o.ProdutosOs).ThenInclude(p => p.Produto)
            .Include(o => o.ServicosOs).ThenInclude(s => s.ServicoNav)
            .Include(o => o.Anotacoes)
            .Include(o => o.Anexos)
            .Include(o => o.Cobrancas)
            .FirstOrDefaultAsync(o => o.Id == id);

    private void Recalcular(Models.Os os)
    {
        var sub = os.ProdutosOs.Sum(p => p.SubTotal) + os.ServicosOs.Sum(s => s.SubTotal);
        os.ValorDesconto = os.TipoDesconto == "percent"
            ? Math.Round(sub * os.Desconto / 100, 2)
            : os.Desconto;
        os.ValorTotal = sub - os.ValorDesconto;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("vOs")) return SemPermissao();
        await SetLayoutDataAsync();
        var os = await CarregarAsync(id);
        if (os is null) return NotFound();
        Os = os;
        Produtos = await Db.Produtos.OrderBy(p => p.Descricao).ToListAsync();
        Servicos = await Db.Servicos.OrderBy(s => s.Nome).ToListAsync();
        MpConfigurado = !string.IsNullOrWhiteSpace(configuration["MercadoPago:AccessToken"]);
        return Page();
    }

    public async Task<IActionResult> OnPostAdicionarProdutoAsync(int id)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        var os = await CarregarAsync(id);
        if (os is null) return NotFound();
        var prod = await Db.Produtos.FindAsync(AddProdutoId);
        if (prod is null) return RedirectToPage(new { id });
        var item = new ProdutoOs { OsId = id, ProdutoId = AddProdutoId, Descricao = prod.Descricao, Quantidade = AddProdQtd, Preco = AddProdPreco > 0 ? AddProdPreco : prod.PrecoVenda };
        item.SubTotal = item.Quantidade * item.Preco;
        Db.ProdutosOs.Add(item);
        os.ProdutosOs.Add(item);
        Recalcular(os);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostExcluirProdutoAsync(int id, int itemId)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        var item = await Db.ProdutosOs.FindAsync(itemId);
        if (item is not null) Db.ProdutosOs.Remove(item);
        var os = await CarregarAsync(id);
        if (os is not null) Recalcular(os);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAdicionarServicoAsync(int id)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        var os = await CarregarAsync(id);
        if (os is null) return NotFound();
        var svc = await Db.Servicos.FindAsync(AddServicoId);
        if (svc is null) return RedirectToPage(new { id });
        var item = new ServicoOs { OsId = id, ServicoId = AddServicoId, Servico = svc.Nome, Quantidade = AddServQtd, Preco = AddServPreco > 0 ? AddServPreco : svc.Preco };
        item.SubTotal = (decimal)item.Quantidade * item.Preco;
        Db.ServicosOs.Add(item);
        os.ServicosOs.Add(item);
        Recalcular(os);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostExcluirServicoAsync(int id, int itemId)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        var item = await Db.ServicosOs.FindAsync(itemId);
        if (item is not null) Db.ServicosOs.Remove(item);
        var os = await CarregarAsync(id);
        if (os is not null) Recalcular(os);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAdicionarAnotacaoAsync(int id)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        if (!string.IsNullOrWhiteSpace(Anotacao))
        {
            Db.AnotacoesOs.Add(new AnotacaoOs { OsId = id, Anotacao = Anotacao, DataHora = DateTime.Now });
            await Db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostExcluirAnotacaoAsync(int id, int anotacaoId)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        var a = await Db.AnotacoesOs.FindAsync(anotacaoId);
        if (a is not null) Db.AnotacoesOs.Remove(a);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAnexarAsync(int id, IFormFile file)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        if (file is { Length: > 0 })
        {
            var dir = Path.Combine("wwwroot", "uploads", "os", id.ToString());
            Directory.CreateDirectory(dir);
            var fileName = Path.GetFileName(file.FileName);
            var path = Path.Combine(dir, fileName);
            using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream);
            Db.Anexos.Add(new Anexo { OsId = id, NomeArquivo = fileName, Url = $"/uploads/os/{id}/{fileName}", Path = path });
            await Db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostExcluirAnexoAsync(int id, int anexoId)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        var a = await Db.Anexos.FindAsync(anexoId);
        if (a is not null) { Db.Anexos.Remove(a); if (a.Path is not null && System.IO.File.Exists(a.Path)) System.IO.File.Delete(a.Path); }
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAdicionarDescontoAsync(int id)
    {
        if (!await TemPermissaoAsync("eOs")) return SemPermissao();
        var os = await CarregarAsync(id);
        if (os is null) return NotFound();
        os.Desconto     = DescontoValor;
        os.TipoDesconto = TipoDesconto;
        Recalcular(os);
        await Db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostGerarBoletoAsync(int id)
    {
        if (!await TemPermissaoAsync("aCobranca")) return SemPermissao();

        var (ok, erro) = await mpSvc.GerarBoletoAsync(id, "os");
        TempData[ok ? "Success" : "Error"] = ok ? "Boleto gerado com sucesso." : erro;
        if (ok) await AuditarAsync($"Gerou boleto MercadoPago para OS #{id}");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostFaturarAsync(int id)
    {
        if (!await TemPermissaoAsync("aLancamento")) return SemPermissao();
        var os = await CarregarAsync(id);
        if (os is null) return NotFound();
        if (os.Faturado) return RedirectToPage(new { id });
        var lancamento = new Lancamento
        {
            Descricao         = $"OS #{os.Id}",
            Valor             = os.ValorTotal,
            ValorDesconto     = os.ValorTotal,
            DataVencimento    = DateOnly.FromDateTime(DateTime.Today),
            Tipo              = "receita",
            ClienteFornecedor = os.Cliente.NomeCliente,
            ClienteId         = os.ClienteId,
            UsuarioId         = UsuarioId,
        };
        Db.Lancamentos.Add(lancamento);
        await Db.SaveChangesAsync();
        os.Faturado    = true;
        os.LancamentoId = lancamento.Id;
        await Db.SaveChangesAsync();
        await AuditarAsync($"Faturou OS #{id}");
        TempData["Success"] = "OS faturada com sucesso.";
        return RedirectToPage(new { id });
    }
}
