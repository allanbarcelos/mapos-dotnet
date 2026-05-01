using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace mapos_dotnet.Pages.Pdv;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IPdvPrinterService printer,
    IPdvPointService point,
    IFiscalCodigoProtector fiscalProtector)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    public SessaoCaixa Sessao { get; set; } = null!;

    // Garante que toda esta página só é acessível via sessão PDV
    public override async Task OnPageHandlerExecutionAsync(
        Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext ctx,
        Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next)
    {
        if (!SessaoPdv)
        {
            ctx.Result = SomenteSessionPdv();
            return;
        }
        await next();
    }

    // ── GET: Frente de caixa ──────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("vPdv")) return SemPermissao();

        var sessao = await Db.SessoesCaixa
            .Include(s => s.Operador)
            .FirstOrDefaultAsync(s => s.OperadorId == UsuarioId && s.Status == "aberta");

        if (sessao is null)
            return RedirectToPage("AbrirCaixa");

        Sessao = sessao;
        await SetLayoutDataAsync();
        return Page();
    }

    // ── GET: Buscar produto (AJAX) ────────────────────────────────────────────
    public async Task<IActionResult> OnGetBuscarProdutoAsync(string q)
    {
        if (!await TemPermissaoAsync("vPdv")) return Forbid();
        if (string.IsNullOrWhiteSpace(q)) return new JsonResult(Array.Empty<object>());

        var t = q.Trim().ToLower();
        var produtos = await Db.Produtos.AsNoTracking()
            .Where(p => p.Descricao.ToLower().Contains(t)
                     || (p.CodDeBarra != null && p.CodDeBarra == t))
            .OrderBy(p => p.Descricao)
            .Take(10)
            .Select(p => new
            {
                p.Id,
                p.Descricao,
                p.PrecoVenda,
                p.Estoque,
                p.CodDeBarra,
                p.Unidade
            })
            .ToListAsync();

        return new JsonResult(produtos);
    }

    // ── POST: Finalizar venda (AJAX) ─────────────────────────────────────────
    public async Task<IActionResult> OnPostFinalizarVendaAsync([FromBody] FinalizarVendaRequest req)
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        var sessao = await Db.SessoesCaixa
            .FirstOrDefaultAsync(s => s.OperadorId == UsuarioId && s.Status == "aberta");

        if (sessao is null)
            return new JsonResult(new { ok = false, erro = "Nenhuma sessão de caixa aberta." });

        if (req.Itens is null || req.Itens.Count == 0)
            return new JsonResult(new { ok = false, erro = "Carrinho vazio." });

        var configs = await ConfigSvc.GetAllAsync();
        var gerarLancamento = configs.GetValueOrDefault("pdv_gerar_lancamento", "1") == "1";
        var controlEstoque  = configs.GetValueOrDefault("control_estoque", "1") == "1";

        // Calcular totais
        decimal subTotal = req.Itens.Sum(i => i.Preco * i.Quantidade);
        decimal desconto = req.TipoDesconto == "porcento"
            ? Math.Round(subTotal * req.Desconto / 100, 2)
            : req.Desconto;
        decimal total = subTotal - desconto;

        if (total <= 0)
            return new JsonResult(new { ok = false, erro = "Valor total inválido." });

        // Buscar ou criar cliente "Consumidor Final"
        int clienteId = req.ClienteId ?? await ObterClienteConsumidorFinalAsync();

        await using var tx = await Db.Database.BeginTransactionAsync();

        // Criar venda
        var venda = new Venda
        {
            DataVenda       = DateOnly.FromDateTime(DateTime.Today),
            ClienteId       = clienteId,
            UsuarioId       = UsuarioId,
            SessaoCaixaId   = sessao.Id,
            FormaPagamento  = req.FormaPagamento,
            ValorTotal      = total,
            Desconto        = req.Desconto,
            ValorDesconto   = desconto,
            TipoDesconto    = req.TipoDesconto ?? "real",
            Status          = "Faturado",
            Faturado        = true,
            Observacoes     = req.Observacoes,
        };

        Db.Vendas.Add(venda);
        await Db.SaveChangesAsync(); // get venda.Id

        // Adicionar itens
        foreach (var item in req.Itens)
        {
            var produto = await Db.Produtos.FindAsync(item.ProdutoId);
            if (produto is null) continue;

            Db.ItensVenda.Add(new ItemVenda
            {
                VendaId    = venda.Id,
                ProdutoId  = item.ProdutoId,
                Quantidade = (int)item.Quantidade,
                Preco      = item.Preco,
                SubTotal   = item.Preco * item.Quantidade,
            });

            if (controlEstoque)
                produto.Estoque -= (int)item.Quantidade;
        }

        // Atualizar saldo esperado da sessão
        if (req.FormaPagamento?.Equals("dinheiro", StringComparison.OrdinalIgnoreCase) == true)
            sessao.SaldoEsperado += total;

        // Gerar lançamento financeiro
        if (gerarLancamento)
        {
            var lancamento = new Lancamento
            {
                Descricao      = $"PDV Venda #{venda.Id}",
                Valor          = subTotal,
                ValorDesconto  = total,
                TipoDesconto   = req.TipoDesconto ?? "real",
                Desconto       = req.Desconto,
                DataVencimento = DateOnly.FromDateTime(DateTime.Today),
                DataPagamento  = DateOnly.FromDateTime(DateTime.Today),
                Baixado        = true,
                Tipo           = "receita",
                FormaPgto      = req.FormaPagamento,
                ClienteId      = clienteId,
                UsuarioId      = UsuarioId,
                VendaId        = venda.Id,
            };
            Db.Lancamentos.Add(lancamento);
        }

        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        await AuditarAsync($"PDV: Finalizou venda #{venda.Id} — R${total:F2} ({req.FormaPagamento})");

        // Pagamento via maquininha MP Point?
        var formasPoint = new[] { "pix", "debito", "credito" };
        if (formasPoint.Contains(req.FormaPagamento?.ToLowerInvariant()))
        {
            var cfg = await ConfigSvc.GetAllAsync();
            var deviceId = cfg.GetValueOrDefault("pdv_mp_device_id", "");
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                var (intentOk, intentId, intentErro) = await point.CriarIntencaoAsync(
                    total,
                    $"PDV Venda #{venda.Id}",
                    req.FormaPagamento!,
                    $"venda-{venda.Id}");

                if (intentOk)
                    return new JsonResult(new { ok = true, vendaId = venda.Id, total, mpPoint = true, paymentIntentId = intentId });

                // Se falhar a criação da intent, avisa mas não bloqueia a venda
                return new JsonResult(new { ok = true, vendaId = venda.Id, total, mpPoint = false, mpErro = intentErro });
            }
        }

        return new JsonResult(new { ok = true, vendaId = venda.Id, total, mpPoint = false });
    }

    // ── GET: Status da intenção de pagamento MP Point ─────────────────────────
    public async Task<IActionResult> OnGetStatusPagamentoAsync(string id)
    {
        if (!await TemPermissaoAsync("vPdv")) return Forbid();
        var status = await point.ConsultarAsync(id);
        return new JsonResult(new
        {
            status  = status.Status,
            mpId    = status.MpPaymentId,
            estado  = status.Estado,
        });
    }

    // ── POST: Cancelar intenção de pagamento MP Point ─────────────────────────
    public async Task<IActionResult> OnPostCancelarPagamentoAsync()
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false });
        var cfg = await ConfigSvc.GetAllAsync();
        var deviceId = cfg.GetValueOrDefault("pdv_mp_device_id", "");
        await point.CancelarAsync(deviceId);
        return new JsonResult(new { ok = true });
    }

    // ── POST: Imprimir cupom via socket ───────────────────────────────────────
    public async Task<IActionResult> OnPostImprimirAsync([FromBody] ImprimirRequest req)
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        var (ok, erro) = await printer.ImprimirCupomAsync(req.VendaId);
        return new JsonResult(new { ok, erro });
    }

    // ── POST: Pausar terminal ─────────────────────────────────────────────────
    public async Task<IActionResult> OnPostPausarAsync()
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        var sessao = await Db.SessoesCaixa
            .Include(s => s.PdvTerminal)
            .FirstOrDefaultAsync(s => s.OperadorId == UsuarioId && s.Status == "aberta");

        if (sessao is null)
            return new JsonResult(new { ok = false, erro = "Nenhuma sessão ativa." });

        if (sessao.PdvTerminal is not null && sessao.PdvTerminal.Status != "pausado")
            sessao.PdvTerminal.Status = "pausado";

        Db.PdvPausas.Add(new PdvPausa
        {
            SessaoCaixaId = sessao.Id,
            IniciadaEm    = DateTime.UtcNow,
        });

        await Db.SaveChangesAsync();
        await AuditarAsync($"PDV: Pausou terminal (sessão #{sessao.Id})");
        return new JsonResult(new { ok = true });
    }

    // ── POST: Retomar terminal (validar senha) ────────────────────────────────
    public async Task<IActionResult> OnPostRetomarAsync([FromBody] RetomarRequest req)
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        var usuario = await Db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == UsuarioId);

        if (usuario is null || string.IsNullOrEmpty(usuario.Senha) ||
            !BCrypt.Net.BCrypt.Verify(req.Pin, usuario.Senha))
        {
            return new JsonResult(new { ok = false, erro = "PIN incorreto." });
        }

        var sessao = await Db.SessoesCaixa
            .Include(s => s.PdvTerminal)
            .FirstOrDefaultAsync(s => s.OperadorId == UsuarioId && s.Status == "aberta");

        if (sessao is null)
            return new JsonResult(new { ok = false, erro = "Sessão não encontrada." });

        // Fechar a pausa aberta
        var pausa = await Db.PdvPausas
            .Where(p => p.SessaoCaixaId == sessao.Id && p.RetomadaEm == null)
            .FirstOrDefaultAsync();

        if (pausa is not null)
            pausa.RetomadaEm = DateTime.UtcNow;

        if (sessao.PdvTerminal is not null)
            sessao.PdvTerminal.Status = "ocupado";

        await Db.SaveChangesAsync();
        await AuditarAsync($"PDV: Retomou terminal após pausa (sessão #{sessao.Id})");
        return new JsonResult(new { ok = true });
    }

    // ── POST: Autorização fiscal PDV ─────────────────────────────────────────
    public async Task<IActionResult> OnPostAutorizarFiscalAsync([FromBody] AutorizarFiscalRequest req)
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        if (string.IsNullOrWhiteSpace(req.Codigo) || string.IsNullOrWhiteSpace(req.Pin))
            return new JsonResult(new { ok = false, erro = "Código e PIN são obrigatórios." });

        // Busca todos os fiscais ativos e verifica o código via descriptografia
        // (não é possível fazer WHERE direto pois o código está criptografado no banco)
        var candidatos = await Db.Usuarios.AsNoTracking()
            .Where(u => u.FiscalPdv && u.Situacao && u.FiscalPdvCodigo != null)
            .ToListAsync();

        var usuario = candidatos.FirstOrDefault(u =>
            fiscalProtector.Verificar(req.Codigo, u.FiscalPdvCodigo!)
            || u.FiscalPdvCodigo == req.Codigo); // fallback para registros legados (plaintext)

        if (usuario is null)
            return new JsonResult(new { ok = false, erro = "Cartão fiscal não reconhecido." });

        if (string.IsNullOrEmpty(usuario.FiscalPdvPin) ||
            !BCrypt.Net.BCrypt.Verify(req.Pin, usuario.FiscalPdvPin))
            return new JsonResult(new { ok = false, erro = "PIN incorreto." });

        // Verificar expiração do PIN (primeiro uso ou > 15 dias)
        var pinExpirado = usuario.PinPrimeiroUso
            || !usuario.PinAlteradoEm.HasValue
            || (DateTime.UtcNow - usuario.PinAlteradoEm.Value).TotalDays > 15;

        if (pinExpirado)
            return new JsonResult(new { ok = false, pinExpirado = true, fiscalId = usuario.Id,
                motivo = usuario.PinPrimeiroUso ? "primeiro_uso" : "expirado" });

        await AuditarAsync($"PDV: Autorização fiscal por {usuario.Nome} (ação: {req.Acao})");
        return new JsonResult(new { ok = true, fiscal = usuario.Nome });
    }

    // ── POST: Alterar PIN fiscal ──────────────────────────────────────────────
    public async Task<IActionResult> OnPostAlterarPinFiscalAsync([FromBody] AlterarPinFiscalRequest req)
    {
        if (!await TemPermissaoAsync("vPdv"))
            return new JsonResult(new { ok = false, erro = "Sem permissão." });

        if (req.FiscalId <= 0 || string.IsNullOrWhiteSpace(req.PinAtual) || string.IsNullOrWhiteSpace(req.PinNovo))
            return new JsonResult(new { ok = false, erro = "Dados incompletos." });

        if (req.PinNovo.Length < 4 || req.PinNovo.Length > 10)
            return new JsonResult(new { ok = false, erro = "O novo PIN deve ter entre 4 e 10 dígitos." });

        var usuario = await Db.Usuarios.FindAsync(req.FiscalId);
        if (usuario is null || !usuario.FiscalPdv || !usuario.Situacao)
            return new JsonResult(new { ok = false, erro = "Fiscal não encontrado." });

        // Verificar código para confirmar identidade
        if (!fiscalProtector.Verificar(req.Codigo, usuario.FiscalPdvCodigo ?? "")
            && usuario.FiscalPdvCodigo != req.Codigo)
            return new JsonResult(new { ok = false, erro = "Código do cartão não confere." });

        // Verificar PIN atual
        if (string.IsNullOrEmpty(usuario.FiscalPdvPin) ||
            !BCrypt.Net.BCrypt.Verify(req.PinAtual, usuario.FiscalPdvPin))
            return new JsonResult(new { ok = false, erro = "PIN atual incorreto." });

        usuario.FiscalPdvPin   = BCrypt.Net.BCrypt.HashPassword(req.PinNovo);
        usuario.PinAlteradoEm  = DateTime.UtcNow;
        usuario.PinPrimeiroUso = false;
        await Db.SaveChangesAsync();
        await AuditarAsync($"PDV: Fiscal {usuario.Nome} alterou o PIN");

        return new JsonResult(new { ok = true });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<int> ObterClienteConsumidorFinalAsync()
    {
        var cf = await Db.Clientes.FirstOrDefaultAsync(c => c.NomeCliente == "Consumidor Final");
        if (cf is not null) return cf.Id;

        cf = new Cliente { NomeCliente = "Consumidor Final" };
        Db.Clientes.Add(cf);
        await Db.SaveChangesAsync();
        return cf.Id;
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────
public class FinalizarVendaRequest
{
    public int? ClienteId      { get; set; }
    public List<ItemVendaReq> Itens { get; set; } = [];
    public decimal Desconto    { get; set; }
    public string? TipoDesconto{ get; set; } = "real";
    public string? FormaPagamento { get; set; } = "dinheiro";
    public decimal ValorRecebido  { get; set; }
    public string? Observacoes    { get; set; }
}

public class ItemVendaReq
{
    public int     ProdutoId  { get; set; }
    public decimal Quantidade { get; set; }
    public decimal Preco      { get; set; }
}

public class ImprimirRequest
{
    public int VendaId { get; set; }
}

public class AutorizarFiscalRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Pin    { get; set; } = string.Empty;
    public string? Acao  { get; set; }
}

public class RetomarRequest
{
    public string Pin { get; set; } = string.Empty;
}

public class AlterarPinFiscalRequest
{
    public int    FiscalId { get; set; }
    public string Codigo   { get; set; } = string.Empty;
    public string PinAtual { get; set; } = string.Empty;
    public string PinNovo  { get; set; } = string.Empty;
}
