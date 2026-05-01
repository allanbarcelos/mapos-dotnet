using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class CadastrarModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [MaxLength(100)]
        public string NomeCliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmação é obrigatória")]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem")]
        [DataType(DataType.Password)]
        public string ConfirmarSenha { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? Celular { get; set; }

        [MaxLength(14)]
        public string? Documento { get; set; }

        [MaxLength(100)]
        public string? Rua { get; set; }

        [MaxLength(10)]
        public string? Numero { get; set; }

        [MaxLength(60)]
        public string? Bairro { get; set; }

        [MaxLength(60)]
        public string? Cidade { get; set; }

        [MaxLength(2)]
        public string? Estado { get; set; }

        [MaxLength(9)]
        public string? Cep { get; set; }
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Verify email uniqueness
        var exists = await db.Clientes.AnyAsync(c => c.Email == Input.Email);
        if (exists)
        {
            ModelState.AddModelError("Input.Email", "Este e-mail já está cadastrado.");
            return Page();
        }

        var cliente = new Cliente
        {
            NomeCliente  = Input.NomeCliente,
            Email        = Input.Email,
            Senha        = BCrypt.Net.BCrypt.HashPassword(Input.Senha),
            Telefone     = Input.Telefone,
            Celular      = Input.Celular,
            Documento    = Input.Documento,
            Rua          = Input.Rua,
            Numero       = Input.Numero,
            Bairro       = Input.Bairro,
            Cidade       = Input.Cidade,
            Estado       = Input.Estado,
            Cep          = Input.Cep,
            DataCadastro = DateOnly.FromDateTime(DateTime.Today),
        };

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        TempData["Success"] = "Cadastro realizado com sucesso! Faça login para continuar.";
        return RedirectToPage("Login");
    }
}
