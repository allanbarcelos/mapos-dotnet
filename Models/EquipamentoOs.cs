namespace mapos_dotnet.Models;

public class EquipamentoOs
{
    public int Id { get; set; }
    public string? DefeitoDeclarado { get; set; }
    public string? DefeitoEncontrado { get; set; }
    public string? Solucao { get; set; }
    public int EquipamentoId { get; set; }
    public int OsId { get; set; }

    public Equipamento Equipamento { get; set; } = null!;
    public Os Os { get; set; } = null!;
}
