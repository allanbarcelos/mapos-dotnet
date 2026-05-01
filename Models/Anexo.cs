namespace mapos_dotnet.Models;

public class Anexo
{
    public int Id { get; set; }
    public string? NomeArquivo { get; set; }
    public string? Thumb { get; set; }
    public string? Url { get; set; }
    public string? Path { get; set; }
    public int OsId { get; set; }

    public Os Os { get; set; } = null!;
}
