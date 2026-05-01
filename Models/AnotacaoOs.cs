namespace mapos_dotnet.Models;

public class AnotacaoOs
{
    public int Id { get; set; }
    public string Anotacao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
    public int OsId { get; set; }

    public Os Os { get; set; } = null!;
}
