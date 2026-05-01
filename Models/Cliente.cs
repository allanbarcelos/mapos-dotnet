namespace mapos_dotnet.Models;

public class Cliente
{
    public int Id { get; set; }
    public string? AsaasId { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public string? Sexo { get; set; }
    public bool PessoaFisica { get; set; } = true;
    public string? Documento { get; set; }
    public string? Telefone { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public string? Senha { get; set; }
    public DateOnly? DataCadastro { get; set; }
    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }
    public string? Contato { get; set; }
    public string? Complemento { get; set; }
    public bool Fornecedor { get; set; }

    public ICollection<Os> OsList { get; set; } = [];
    public ICollection<Venda> Vendas { get; set; } = [];
    public ICollection<Lancamento> Lancamentos { get; set; } = [];
    public ICollection<Cobranca> Cobrancas { get; set; } = [];
    public ICollection<Equipamento> Equipamentos { get; set; } = [];
}
