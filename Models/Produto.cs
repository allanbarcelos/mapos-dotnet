namespace mapos_dotnet.Models;

public class Produto
{
    public int Id { get; set; }
    public string? CodDeBarra { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal PrecoCompra { get; set; }
    public decimal PrecoVenda { get; set; }
    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public bool Saida { get; set; } = true;
    public bool Entrada { get; set; } = true;
    public int? CategoriaId { get; set; }

    public Categoria? Categoria { get; set; }
    public ICollection<ProdutoOs> ProdutosOs { get; set; } = [];
    public ICollection<ItemVenda> ItensVenda { get; set; } = [];
}
