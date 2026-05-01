namespace mapos_dotnet.Models;

public class Conta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Banco { get; set; }
    public string? NumeroAgencia { get; set; }
    public decimal Saldo { get; set; }
    public DateOnly? Cadastro { get; set; }
    public bool Status { get; set; } = true;
    public string? Tipo { get; set; }

    public ICollection<Lancamento> Lancamentos { get; set; } = [];
}
