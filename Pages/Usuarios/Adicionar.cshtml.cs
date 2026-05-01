using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Usuarios;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IFiscalCodigoProtector fiscalProtector) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Permissao> Permissoes { get; private set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(80)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(80)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Rg { get; set; }

        [MaxLength(20)]
        public string? Cpf { get; set; }

        [MaxLength(9)]
        public string? Cep { get; set; }

        [MaxLength(70)]
        public string? Rua { get; set; }

        [MaxLength(15)]
        public string? Numero { get; set; }

        [MaxLength(45)]
        public string? Bairro { get; set; }

        [MaxLength(45)]
        public string? Cidade { get; set; }

        [MaxLength(20)]
        public string? Estado { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(20)]
        public string? Celular { get; set; }

        [Required(ErrorMessage = "Selecione uma permissão.")]
        public int PermissaoId { get; set; }

        public DateOnly? DataExpiracao { get; set; }

        public bool Situacao { get; set; } = true;

        // PDV — valores: "nenhum" | "operador" | "fiscal"
        public string FuncaoPdv { get; set; } = "nenhum";

        [MinLength(4, ErrorMessage = "PIN deve ter no mínimo 4 dígitos.")]
        [MaxLength(10)]
        public string? FiscalPdvPin { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cUsuario"))
            return SemPermissao();

        await SetLayoutDataAsync();
        await CarregarPermissoesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("cUsuario"))
            return SemPermissao();

        await CarregarPermissoesAsync();

        if (!ModelState.IsValid)
        {
            await SetLayoutDataAsync();
            return Page();
        }

        var emailExiste = await Db.Usuarios.AnyAsync(u => u.Email == Input.Email);
        if (emailExiste)
        {
            ModelState.AddModelError("Input.Email", "Este e-mail já está em uso.");
            await SetLayoutDataAsync();
            return Page();
        }

        var usuario = new Usuario
        {
            Nome          = Input.Nome,
            Email         = Input.Email,
            Senha         = BCrypt.Net.BCrypt.HashPassword(Input.Senha),
            Rg            = Input.Rg,
            Cpf           = Input.Cpf,
            Cep           = Input.Cep,
            Rua           = Input.Rua,
            Numero        = Input.Numero,
            Bairro        = Input.Bairro,
            Cidade        = Input.Cidade,
            Estado        = Input.Estado,
            Telefone      = Input.Telefone,
            Celular       = Input.Celular,
            PermissaoId   = Input.PermissaoId,
            DataExpiracao = Input.DataExpiracao,
            Situacao      = Input.Situacao,
            DataCadastro  = DateOnly.FromDateTime(DateTime.Today),
        };

        // PDV
        usuario.FiscalPdv     = Input.FuncaoPdv == "fiscal";
        usuario.OperadorCaixa = Input.FuncaoPdv == "operador";

        if (usuario.FiscalPdv)
        {
            if (string.IsNullOrWhiteSpace(Input.FiscalPdvPin))
            {
                ModelState.AddModelError("Input.FiscalPdvPin", "Informe o PIN para habilitar o Fiscal PDV.");
                await SetLayoutDataAsync();
                return Page();
            }
            var codigoGerado        = $"FISCAL{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            usuario.FiscalPdvCodigo = fiscalProtector.Proteger(codigoGerado);
            usuario.FiscalPdvPin    = BCrypt.Net.BCrypt.HashPassword(Input.FiscalPdvPin);
        }

        Db.Usuarios.Add(usuario);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Cadastrou usuário #{usuario.Id} - {usuario.Nome}");

        TempData["Success"] = $"Usuário \"{usuario.Nome}\" cadastrado com sucesso.";
        return RedirectToPage("/Usuarios/Index");
    }

    private async Task CarregarPermissoesAsync()
    {
        Permissoes = await Db.Permissoes
            .Where(p => p.Situacao)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }
}
