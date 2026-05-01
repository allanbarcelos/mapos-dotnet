using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Clientes;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(200)]
        public string NomeCliente { get; set; } = string.Empty;

        public bool PessoaFisica { get; set; } = true;

        [MaxLength(10)]
        public string? Sexo { get; set; }

        [MaxLength(20)]
        public string? Documento { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(20)]
        public string? Celular { get; set; }

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(150)]
        public string? Email { get; set; }

        public DateOnly? DataCadastro { get; set; }

        [MaxLength(200)]
        public string? Rua { get; set; }

        [MaxLength(10)]
        public string? Numero { get; set; }

        [MaxLength(100)]
        public string? Bairro { get; set; }

        [MaxLength(100)]
        public string? Cidade { get; set; }

        [MaxLength(2)]
        public string? Estado { get; set; }

        [MaxLength(10)]
        public string? Cep { get; set; }

        [MaxLength(100)]
        public string? Contato { get; set; }

        [MaxLength(200)]
        public string? Complemento { get; set; }

        public bool Fornecedor { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("eCliente"))
            return SemPermissao();

        await SetLayoutDataAsync();

        var cliente = await Db.Clientes.FindAsync(Id);
        if (cliente is null)
        {
            TempData["Error"] = "Cliente não encontrado.";
            return RedirectToPage("/Clientes/Index");
        }

        Input = new InputModel
        {
            NomeCliente  = cliente.NomeCliente,
            PessoaFisica = cliente.PessoaFisica,
            Sexo         = cliente.Sexo,
            Documento    = cliente.Documento,
            Telefone     = cliente.Telefone,
            Celular      = cliente.Celular,
            Email        = cliente.Email,
            DataCadastro = cliente.DataCadastro,
            Rua          = cliente.Rua,
            Numero       = cliente.Numero,
            Bairro       = cliente.Bairro,
            Cidade       = cliente.Cidade,
            Estado       = cliente.Estado,
            Cep          = cliente.Cep,
            Contato      = cliente.Contato,
            Complemento  = cliente.Complemento,
            Fornecedor   = cliente.Fornecedor,
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eCliente"))
            return SemPermissao();

        await SetLayoutDataAsync();

        if (!ModelState.IsValid)
            return Page();

        var cliente = await Db.Clientes.FindAsync(Id);
        if (cliente is null)
        {
            TempData["Error"] = "Cliente não encontrado.";
            return RedirectToPage("/Clientes/Index");
        }

        cliente.NomeCliente  = Input.NomeCliente.Trim();
        cliente.PessoaFisica = Input.PessoaFisica;
        cliente.Sexo         = Input.Sexo;
        cliente.Documento    = Input.Documento?.Trim();
        cliente.Telefone     = Input.Telefone?.Trim();
        cliente.Celular      = Input.Celular?.Trim();
        cliente.Email        = Input.Email?.Trim();
        cliente.DataCadastro = Input.DataCadastro;
        cliente.Rua          = Input.Rua?.Trim();
        cliente.Numero       = Input.Numero?.Trim();
        cliente.Bairro       = Input.Bairro?.Trim();
        cliente.Cidade       = Input.Cidade?.Trim();
        cliente.Estado       = Input.Estado;
        cliente.Cep          = Input.Cep?.Trim();
        cliente.Contato      = Input.Contato?.Trim();
        cliente.Complemento  = Input.Complemento?.Trim();
        cliente.Fornecedor   = Input.Fornecedor;

        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou cliente #{cliente.Id} - {cliente.NomeCliente}");

        TempData["Success"] = $"Cliente \"{cliente.NomeCliente}\" atualizado com sucesso.";
        return RedirectToPage("/Clientes/Index");
    }
}
