namespace mapos_dotnet.Models;

public class Log
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Tarefa { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public TimeOnly Hora { get; set; }
    public string? Ip { get; set; }
    public int? UsuarioId { get; set; }

    public Usuario? UsuarioNav { get; set; }
}
