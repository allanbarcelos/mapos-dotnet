namespace mapos_dotnet.Models;

public class SessaoCaixa
{
    public int Id { get; set; }
    public DateTime AbertoEm { get; set; }
    public DateTime? FechadoEm { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal SaldoEsperado { get; set; }
    public decimal? SaldoInformado { get; set; }
    public decimal? Diferenca { get; set; }
    public string Status { get; set; } = "aberta"; // aberta | fechada
    public string? Observacoes { get; set; }
    public int OperadorId { get; set; }

    public int? PdvTerminalId { get; set; }
    public PdvTerminal? PdvTerminal { get; set; }
    public ICollection<PdvPausa> Pausas { get; set; } = [];

    public Usuario Operador { get; set; } = null!;
    public ICollection<Venda> Vendas { get; set; } = [];
}
