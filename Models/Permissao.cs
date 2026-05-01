namespace mapos_dotnet.Models;

public class Permissao
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Permissoes { get; set; } // JSON
    public bool Situacao { get; set; } = true;
    public DateOnly? Data { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = [];
}
