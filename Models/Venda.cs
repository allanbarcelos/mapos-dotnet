namespace mapos_dotnet.Models;

public class Venda
{
    public int Id { get; set; }
    public DateOnly DataVenda { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public string TipoDesconto { get; set; } = "real";
    public bool Faturado { get; set; }
    public string? Observacoes { get; set; }
    public string? ObservacoesCliente { get; set; }
    public int ClienteId { get; set; }
    public int? UsuarioId { get; set; }
    public int? LancamentoId { get; set; }
    public string Status { get; set; } = "Aberto";
    public int? GarantiaId { get; set; }
    public int? SessaoCaixaId { get; set; }
    public string? FormaPagamento { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Usuario? Usuario { get; set; }
    public Lancamento? Lancamento { get; set; }
    public SessaoCaixa? SessaoCaixa { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = [];
    public ICollection<Cobranca> Cobrancas { get; set; } = [];
}
