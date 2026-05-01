using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Clientes;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
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
        if (!await TemPermissaoAsync("aCliente"))
            return SemPermissao();

        await SetLayoutDataAsync();
        Input.DataCadastro = DateOnly.FromDateTime(DateTime.Today);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aCliente"))
            return SemPermissao();

        await SetLayoutDataAsync();

        if (!ModelState.IsValid)
            return Page();

        var cliente = new Cliente
        {
            NomeCliente  = Input.NomeCliente.Trim(),
            PessoaFisica = Input.PessoaFisica,
            Sexo         = Input.Sexo,
            Documento    = Input.Documento?.Trim(),
            Telefone     = Input.Telefone?.Trim(),
            Celular      = Input.Celular?.Trim(),
            Email        = Input.Email?.Trim(),
            DataCadastro = Input.DataCadastro,
            Rua          = Input.Rua?.Trim(),
            Numero       = Input.Numero?.Trim(),
            Bairro       = Input.Bairro?.Trim(),
            Cidade       = Input.Cidade?.Trim(),
            Estado       = Input.Estado,
            Cep          = Input.Cep?.Trim(),
            Contato      = Input.Contato?.Trim(),
            Complemento  = Input.Complemento?.Trim(),
            Fornecedor   = Input.Fornecedor,
        };

        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Cadastrou cliente #{cliente.Id} - {cliente.NomeCliente}");

        TempData["Success"] = $"Cliente \"{cliente.NomeCliente}\" cadastrado com sucesso.";
        return RedirectToPage("/Clientes/Index");
    }
}
