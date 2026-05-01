namespace mapos_dotnet.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Rg { get; set; }
    public string? Cpf { get; set; }
    public string? Cep { get; set; }
    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Celular { get; set; }
    public bool Situacao { get; set; } = true;
    public DateOnly? DataCadastro { get; set; }
    public int PermissaoId { get; set; }
    public DateOnly? DataExpiracao { get; set; }
    public string? UrlImageUser { get; set; }

    // PDV — funções mutuamente exclusivas
    public bool    FiscalPdv       { get; set; }   // Fiscal: pode autorizar descontos/remoções
    public bool    OperadorCaixa   { get; set; }   // Operador de caixa: pode abrir/fechar caixa
    public string? FiscalPdvCodigo { get; set; }   // unique barcode value (fiscal only)
    public string? FiscalPdvPin    { get; set; }   // bcrypt hash (fiscal only)

    public Permissao Permissao { get; set; } = null!;
    public ICollection<Os> OsList { get; set; } = [];
    public ICollection<Venda> Vendas { get; set; } = [];
    public ICollection<Lancamento> Lancamentos { get; set; } = [];
    public ICollection<Log> Logs { get; set; } = [];
}
