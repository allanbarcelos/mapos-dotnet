namespace mapos_dotnet.Models;

public class Documento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? File { get; set; }
    public string? Path { get; set; }
    public string? Url { get; set; }
    public DateOnly? Cadastro { get; set; }
    public string? Categoria { get; set; }
    public string? Tipo { get; set; }
    public string? Tamanho { get; set; }
}
