namespace mapos_dotnet.Models;

public class Lancamento
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public string TipoDesconto { get; set; } = "real";
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public bool Baixado { get; set; }
    public string? ClienteFornecedor { get; set; }
    public string? FormaPgto { get; set; }
    public string Tipo { get; set; } = "receita"; // receita | despesa
    public string? Anexo { get; set; }
    public string? Observacoes { get; set; }
    public int? ClienteId { get; set; }
    public int? CategoriaId { get; set; }
    public int? ContaId { get; set; }
    public int? VendaId { get; set; }
    public int UsuarioId { get; set; }

    public Cliente? Cliente { get; set; }
    public Categoria? Categoria { get; set; }
    public Conta? Conta { get; set; }
    public Venda? Venda { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public ICollection<Os> OsLancamentos { get; set; } = [];
}
