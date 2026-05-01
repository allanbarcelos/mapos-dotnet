namespace mapos_dotnet.Models;

public class Equipamento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? NumSerie { get; set; }
    public string? Modelo { get; set; }
    public string? Cor { get; set; }
    public string? Descricao { get; set; }
    public string? Tensao { get; set; }
    public string? Potencia { get; set; }
    public string? Voltagem { get; set; }
    public DateOnly? DataFabricacao { get; set; }
    public int? MarcaId { get; set; }
    public int? ClienteId { get; set; }

    public Marca? Marca { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<EquipamentoOs> EquipamentosOs { get; set; } = [];
}
