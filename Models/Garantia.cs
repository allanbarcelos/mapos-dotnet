namespace mapos_dotnet.Models;

public class Garantia
{
    public int Id { get; set; }
    public DateOnly? DataGarantia { get; set; }
    public string? RefGarantia { get; set; }
    public string TextoGarantia { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }
    public ICollection<Os> OsList { get; set; } = [];
}
