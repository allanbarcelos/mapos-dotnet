namespace mapos_dotnet.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateOnly? Cadastro { get; set; }
    public bool Status { get; set; } = true;
    public string? Tipo { get; set; }

    public ICollection<Lancamento> Lancamentos { get; set; } = [];
    public ICollection<Produto> Produtos { get; set; } = [];
}
