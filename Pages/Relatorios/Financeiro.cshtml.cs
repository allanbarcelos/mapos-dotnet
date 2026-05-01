using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Relatorios;

public class FinanceiroModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public List<Lancamento> Items { get; set; } = [];
    public string? Tipo   { get; set; }
    public string? De     { get; set; }
    public string? Ate    { get; set; }
    public bool ApenasNaoBaixados { get; set; }

    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo         => TotalReceitas - TotalDespesas;

    private async Task<bool> CarregarAsync(string? tipo, string? de, string? ate, bool pendentes)
    {
        if (!await TemPermissaoAsync("rFinanceiro")) return false;
        Tipo = tipo; De = de; Ate = ate; ApenasNaoBaixados = pendentes;

        var query = Db.Lancamentos.AsNoTracking()
            .Include(l => l.Cliente)
            .Include(l => l.Categoria)
            .Include(l => l.Conta)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(l => l.Tipo == tipo);
        if (DateOnly.TryParse(de,  out var d)) query = query.Where(l => l.DataVencimento >= d);
        if (DateOnly.TryParse(ate, out var a)) query = query.Where(l => l.DataVencimento <= a);
        if (pendentes) query = query.Where(l => !l.Baixado);

        Items         = await query.OrderByDescending(l => l.DataVencimento).Take(500).ToListAsync();
        TotalReceitas = Items.Where(l => l.Tipo == "receita").Sum(l => l.ValorDesconto);
        TotalDespesas = Items.Where(l => l.Tipo == "despesa").Sum(l => l.ValorDesconto);
        return true;
    }

    public async Task<IActionResult> OnGetAsync(string? tipo, string? de, string? ate, bool pendentes = false)
    {
        if (!await CarregarAsync(tipo, de, ate, pendentes)) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportarAsync(string? tipo, string? de, string? ate, bool pendentes = false)
    {
        if (!await CarregarAsync(tipo, de, ate, pendentes)) return SemPermissao();

        string[] headers = ["#", "Descrição", "Tipo", "Categoria", "Conta", "Cliente/Fornecedor", "Forma Pgto", "Vencimento", "Pagamento", "Baixado", "Valor (R$)"];
        var rows = Items.Select(l => new object?[]
        {
            l.Id,
            l.Descricao,
            l.Tipo,
            l.Categoria?.Nome ?? "",
            l.Conta?.Nome ?? "",
            l.ClienteFornecedor ?? l.Cliente?.NomeCliente ?? "",
            l.FormaPgto ?? "",
            l.DataVencimento,
            l.DataPagamento,
            l.Baixado,
            l.ValorDesconto
        });

        var bytes = ExcelHelper.Gerar("Financeiro", headers, rows);
        return File(bytes, ExcelHelper.ContentType, "relatorio-financeiro.xlsx");
    }
}
