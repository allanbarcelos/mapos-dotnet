namespace mapos_dotnet.Models;

public class Marca
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateOnly? Cadastro { get; set; }
    public bool Situacao { get; set; } = true;

    public ICollection<Equipamento> Equipamentos { get; set; } = [];
}
