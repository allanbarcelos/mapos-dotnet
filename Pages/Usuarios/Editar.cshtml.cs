using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Usuarios;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc,
    IFiscalCodigoProtector fiscalProtector) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Permissao> Permissoes { get; private set; } = [];
    public bool TemCartaoFiscal { get; private set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(80)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(80)]
        public string Email { get; set; } = string.Empty;

        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string? NovaSenha { get; set; }

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
        public string? FiscalPdvPin { get; set; }   // blank = keep existing
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("cUsuario"))
            return SemPermissao();

        await SetLayoutDataAsync();
        await CarregarPermissoesAsync();

        var usuario = await Db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            TempData["Error"] = "Usuário não encontrado.";
            return RedirectToPage("/Usuarios/Index");
        }

        TemCartaoFiscal = usuario.FiscalPdv && !string.IsNullOrEmpty(usuario.FiscalPdvCodigo);

        Input = new InputModel
        {
            Id            = usuario.Id,
            Nome          = usuario.Nome,
            Email         = usuario.Email,
            Rg            = usuario.Rg,
            Cpf           = usuario.Cpf,
            Cep           = usuario.Cep,
            Rua           = usuario.Rua,
            Numero        = usuario.Numero,
            Bairro        = usuario.Bairro,
            Cidade        = usuario.Cidade,
            Estado        = usuario.Estado,
            Telefone      = usuario.Telefone,
            Celular       = usuario.Celular,
            PermissaoId   = usuario.PermissaoId,
            DataExpiracao = usuario.DataExpiracao,
            Situacao      = usuario.Situacao,
            FuncaoPdv     = usuario.FiscalPdv ? "fiscal" : usuario.OperadorCaixa ? "operador" : "nenhum",
        };

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

        var usuario = await Db.Usuarios.FindAsync(Input.Id);
        if (usuario is null)
        {
            TempData["Error"] = "Usuário não encontrado.";
            return RedirectToPage("/Usuarios/Index");
        }

        var emailExiste = await Db.Usuarios
            .AnyAsync(u => u.Email == Input.Email && u.Id != Input.Id);
        if (emailExiste)
        {
            ModelState.AddModelError("Input.Email", "Este e-mail já está em uso.");
            await SetLayoutDataAsync();
            return Page();
        }

        usuario.Nome          = Input.Nome;
        usuario.Email         = Input.Email;
        usuario.Rg            = Input.Rg;
        usuario.Cpf           = Input.Cpf;
        usuario.Cep           = Input.Cep;
        usuario.Rua           = Input.Rua;
        usuario.Numero        = Input.Numero;
        usuario.Bairro        = Input.Bairro;
        usuario.Cidade        = Input.Cidade;
        usuario.Estado        = Input.Estado;
        usuario.Telefone      = Input.Telefone;
        usuario.Celular       = Input.Celular;
        usuario.PermissaoId   = Input.PermissaoId;
        usuario.DataExpiracao = Input.DataExpiracao;
        usuario.Situacao      = Input.Situacao;

        if (!string.IsNullOrWhiteSpace(Input.NovaSenha))
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(Input.NovaSenha);

        // PDV
        usuario.FiscalPdv     = Input.FuncaoPdv == "fiscal";
        usuario.OperadorCaixa = Input.FuncaoPdv == "operador";

        if (usuario.FiscalPdv)
        {
            // Gerar código na primeira vez (protegido com AES)
            if (string.IsNullOrEmpty(usuario.FiscalPdvCodigo))
            {
                var codigoGerado        = $"FISCAL{Guid.NewGuid():N}"[..20].ToUpperInvariant();
                usuario.FiscalPdvCodigo = fiscalProtector.Proteger(codigoGerado);
            }

            // Atualizar PIN somente se informado
            if (!string.IsNullOrWhiteSpace(Input.FiscalPdvPin))
                usuario.FiscalPdvPin = BCrypt.Net.BCrypt.HashPassword(Input.FiscalPdvPin);
            else if (string.IsNullOrEmpty(usuario.FiscalPdvPin))
            {
                ModelState.AddModelError("Input.FiscalPdvPin", "Informe o PIN para habilitar o Fiscal PDV.");
                await SetLayoutDataAsync();
                TemCartaoFiscal = false;
                return Page();
            }
        }
        else
        {
            // Remover dados de fiscal ao trocar de função
            usuario.FiscalPdvCodigo = null;
            usuario.FiscalPdvPin    = null;
        }

        await Db.SaveChangesAsync();
        PermissaoSvc.InvalidarCachePorUsuario(usuario.Id);
        await AuditarAsync($"Editou usuário #{usuario.Id} - {usuario.Nome}");

        TempData["Success"] = $"Usuário \"{usuario.Nome}\" atualizado com sucesso.";
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
