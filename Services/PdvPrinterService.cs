using System.Net.Sockets;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Services;

public interface IPdvPrinterService
{
    Task<(bool ok, string erro)> ImprimirCupomAsync(int vendaId);
    Task<(bool ok, string erro)> ImprimirExtratoAsync(int sessaoId);
    Task<(bool ok, string erro)> ImprimirRelatorioAsync(int sessaoId);
}

public class PdvPrinterService(
    ApplicationDbContext db,
    IConfiguracaoService configSvc,
    EscPosService escPos,
    ILogger<PdvPrinterService> logger) : IPdvPrinterService
{
    public async Task<(bool ok, string erro)> ImprimirCupomAsync(int vendaId)
    {
        var configs = await configSvc.GetAllAsync();

        var host = configs.GetValueOrDefault("pdv_printer_host", "");
        if (string.IsNullOrWhiteSpace(host))
            return (false, "Impressora não configurada. Defina o host em Configurações → PDV.");

        if (!int.TryParse(configs.GetValueOrDefault("pdv_printer_port", "9100"), out var port))
            port = 9100;

        var venda = await db.Vendas
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == vendaId);

        if (venda is null)
            return (false, "Venda não encontrada.");

        var data = new CupomData
        {
            VendaId        = venda.Id,
            DataHora       = DateTime.Now,
            ClienteNome    = venda.Cliente?.NomeCliente,
            Desconto       = venda.ValorDesconto,
            Total          = venda.ValorTotal,
            FormaPagamento = venda.FormaPagamento ?? "dinheiro",
            Itens          = venda.Itens.Select(i => new CupomItem
            {
                Descricao     = i.Produto?.Descricao ?? "Produto",
                Quantidade    = i.Quantidade,
                PrecoUnitario = i.Preco,
                SubTotal      = i.SubTotal,
            }).ToList()
        };

        byte[] bytes;
        try
        {
            bytes = await escPos.GerarCupomAsync(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar ESC/POS para venda {VendaId}", vendaId);
            return (false, $"Erro ao gerar cupom: {ex.Message}");
        }

        return await EnviarViaSocketAsync(host, port, bytes);
    }

    public async Task<(bool ok, string erro)> ImprimirExtratoAsync(int sessaoId)
    {
        var configs = await configSvc.GetAllAsync();

        var host = configs.GetValueOrDefault("pdv_printer_host", "");
        if (string.IsNullOrWhiteSpace(host))
            return (false, "Impressora não configurada. Defina o host em Configurações → PDV.");

        if (!int.TryParse(configs.GetValueOrDefault("pdv_printer_port", "9100"), out var port))
            port = 9100;

        var sessao = await db.SessoesCaixa
            .AsNoTracking()
            .Include(s => s.Operador)
            .Include(s => s.Vendas)
            .FirstOrDefaultAsync(s => s.Id == sessaoId);

        if (sessao is null)
            return (false, "Sessão não encontrada.");

        var data = new ExtratoCaixaData
        {
            SessaoId       = sessao.Id,
            OperadorNome   = sessao.Operador?.Nome ?? "—",
            AbertoEm       = sessao.AbertoEm.ToLocalTime(),
            FechadoEm      = sessao.FechadoEm?.ToLocalTime(),
            QtdVendas      = sessao.Vendas.Count,
            TotalVendas    = sessao.Vendas.Sum(v => v.ValorTotal),
            TotalDinheiro  = sessao.Vendas.Where(v => v.FormaPagamento == "dinheiro").Sum(v => v.ValorTotal),
            TotalPix       = sessao.Vendas.Where(v => v.FormaPagamento == "pix").Sum(v => v.ValorTotal),
            TotalDebito    = sessao.Vendas.Where(v => v.FormaPagamento == "debito").Sum(v => v.ValorTotal),
            TotalCredito   = sessao.Vendas.Where(v => v.FormaPagamento == "credito").Sum(v => v.ValorTotal),
            TotalFiado     = sessao.Vendas.Where(v => v.FormaPagamento == "fiado").Sum(v => v.ValorTotal),
            SaldoInicial   = sessao.SaldoInicial,
            SaldoEsperado  = sessao.SaldoEsperado,
            SaldoInformado = sessao.SaldoInformado,
            Diferenca      = sessao.Diferenca,
        };

        byte[] bytes;
        try
        {
            bytes = await escPos.GerarExtratoAsync(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar extrato ESC/POS para sessão {SessaoId}", sessaoId);
            return (false, $"Erro ao gerar extrato: {ex.Message}");
        }

        return await EnviarViaSocketAsync(host, port, bytes);
    }

    public async Task<(bool ok, string erro)> ImprimirRelatorioAsync(int sessaoId)
    {
        var configs = await configSvc.GetAllAsync();

        var host = configs.GetValueOrDefault("pdv_printer_host", "");
        if (string.IsNullOrWhiteSpace(host))
            return (false, "Impressora não configurada. Defina o host em Configurações → PDV.");

        if (!int.TryParse(configs.GetValueOrDefault("pdv_printer_port", "9100"), out var port))
            port = 9100;

        var sessao = await db.SessoesCaixa
            .AsNoTracking()
            .Include(s => s.Operador)
            .Include(s => s.PdvTerminal)
            .Include(s => s.Pausas)
            .FirstOrDefaultAsync(s => s.Id == sessaoId);

        if (sessao is null)
            return (false, "Sessão não encontrada.");

        var pausas = sessao.Pausas
            .OrderBy(p => p.IniciadaEm)
            .Select(p => new PdvPausaData
            {
                IniciadaEm  = p.IniciadaEm.ToLocalTime(),
                RetomadaEm  = p.RetomadaEm?.ToLocalTime(),
            })
            .ToList();

        var data = new RelatorioSessaoData
        {
            SessaoId     = sessao.Id,
            OperadorNome = sessao.Operador?.Nome ?? "—",
            TerminalNome = sessao.PdvTerminal?.Nome,
            AbertoEm     = sessao.AbertoEm.ToLocalTime(),
            FechadoEm    = sessao.FechadoEm?.ToLocalTime(),
            Pausas       = pausas,
        };

        byte[] bytes;
        try
        {
            bytes = await escPos.GerarRelatorioAsync(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar relatório ESC/POS para sessão {SessaoId}", sessaoId);
            return (false, $"Erro ao gerar relatório: {ex.Message}");
        }

        return await EnviarViaSocketAsync(host, port, bytes);
    }

    private async Task<(bool ok, string erro)> EnviarViaSocketAsync(string host, int port, byte[] data)
    {
        try
        {
            using var tcp    = new TcpClient();
            using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await tcp.ConnectAsync(host, port, cts.Token);

            await using var stream = tcp.GetStream();
            await stream.WriteAsync(data, cts.Token);
            await stream.FlushAsync(cts.Token);

            logger.LogInformation("Cupom enviado para {Host}:{Port} ({Bytes} bytes)", host, port, data.Length);
            return (true, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return (false, $"Timeout ao conectar na impressora {host}:{port}. Verifique o host e porta.");
        }
        catch (SocketException ex)
        {
            logger.LogWarning(ex, "Falha na conexão com impressora {Host}:{Port}", host, port);
            return (false, $"Falha na conexão com a impressora ({host}:{port}): {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado ao imprimir");
            return (false, $"Erro ao imprimir: {ex.Message}");
        }
    }
}
