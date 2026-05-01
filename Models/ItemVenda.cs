namespace mapos_dotnet.Models;

public class ItemVenda
{
    public int Id { get; set; }
    public decimal SubTotal { get; set; }
    public int Quantidade { get; set; }
    public decimal Preco { get; set; }
    public int VendaId { get; set; }
    public int ProdutoId { get; set; }

    public Venda Venda { get; set; } = null!;
    public Produto Produto { get; set; } = null!;
}
