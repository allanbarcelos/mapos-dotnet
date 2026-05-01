namespace mapos_dotnet.Models;

public class ServicoOs
{
    public int Id { get; set; }
    public string Servico { get; set; } = string.Empty;
    public double Quantidade { get; set; }
    public decimal Preco { get; set; }
    public int OsId { get; set; }
    public int ServicoId { get; set; }
    public decimal SubTotal { get; set; }

    public Os Os { get; set; } = null!;
    public Servico ServicoNav { get; set; } = null!;
}
