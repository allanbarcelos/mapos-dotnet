namespace mapos_dotnet.Models;

public class ProdutoOs
{
    public int Id { get; set; }
    public int Quantidade { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int OsId { get; set; }
    public int ProdutoId { get; set; }
    public decimal SubTotal { get; set; }

    public Os Os { get; set; } = null!;
    public Produto Produto { get; set; } = null!;
}
