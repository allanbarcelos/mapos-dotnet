namespace mapos_dotnet.Models;

public class PdvPausa
{
    public int Id { get; set; }
    public int SessaoCaixaId { get; set; }
    public DateTime IniciadaEm { get; set; }
    public DateTime? RetomadaEm { get; set; }
    public SessaoCaixa Sessao { get; set; } = null!;
}
