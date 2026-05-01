namespace mapos_dotnet.Models;

public class PdvTerminal
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;       // varchar(80), required
    public string? Descricao { get; set; }                   // text, optional
    public string Status { get; set; } = "disponivel";      // 'disponivel' | 'ocupado' | 'pausado'
    public int? SessaoCaixaId { get; set; }                  // FK to current session
    public SessaoCaixa? SessaoCaixa { get; set; }
    public ICollection<SessaoCaixa> Sessoes { get; set; } = [];
}
