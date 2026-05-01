namespace mapos_dotnet.Models;

public class Configuracao
{
    public int Id { get; set; }
    public string Config { get; set; } = string.Empty;
    public string? Valor { get; set; }
}
