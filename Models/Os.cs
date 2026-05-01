namespace mapos_dotnet.Models;

public class Os
{
    public int Id { get; set; }
    public DateOnly? DataInicial { get; set; }
    public DateOnly? DataFinal { get; set; }
    public string? Garantia { get; set; }
    public string? DescricaoProduto { get; set; }
    public string? Defeito { get; set; }
    public string Status { get; set; } = "Aberto";
    public string? Observacoes { get; set; }
    public string? LaudoTecnico { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public string TipoDesconto { get; set; } = "real";
    public int ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? LancamentoId { get; set; }
    public bool Faturado { get; set; }
    public int? GarantiaId { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public Lancamento? Lancamento { get; set; }
    public Garantia? GarantiaTermo { get; set; }
    public ICollection<ProdutoOs> ProdutosOs { get; set; } = [];
    public ICollection<ServicoOs> ServicosOs { get; set; } = [];
    public ICollection<EquipamentoOs> EquipamentosOs { get; set; } = [];
    public ICollection<Anexo> Anexos { get; set; } = [];
    public ICollection<AnotacaoOs> Anotacoes { get; set; } = [];
    public ICollection<Cobranca> Cobrancas { get; set; } = [];
}
