using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class EditarDadosModel(ApplicationDbContext db) : MinePageModelBase(db)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(255)]
        public string NomeCliente { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(20)]
        public string? Celular { get; set; }

        [MaxLength(70)]
        public string? Rua { get; set; }

        [MaxLength(15)]
        public string? Numero { get; set; }

        [MaxLength(45)]
        public string? Complemento { get; set; }

        [MaxLength(45)]
        public string? Bairro { get; set; }

        [MaxLength(45)]
        public string? Cidade { get; set; }

        [MaxLength(20)]
        public string? Estado { get; set; }

        [MaxLength(20)]
        public string? Cep { get; set; }

        [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        public string? NovaSenha { get; set; }

        public string? ConfirmarSenha { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var cliente = await Db.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ClienteId);
        if (cliente is null) return NotFound();

        Input = new InputModel
        {
            NomeCliente  = cliente.NomeCliente,
            Telefone     = cliente.Telefone,
            Celular      = cliente.Celular,
            Rua          = cliente.Rua,
            Numero       = cliente.Numero,
            Complemento  = cliente.Complemento,
            Bairro       = cliente.Bairro,
            Cidade       = cliente.Cidade,
            Estado       = cliente.Estado,
            Cep          = cliente.Cep
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrWhiteSpace(Input.NovaSenha) &&
            Input.NovaSenha != Input.ConfirmarSenha)
        {
            ModelState.AddModelError("Input.ConfirmarSenha", "As senhas não coincidem.");
        }

        if (!ModelState.IsValid) return Page();

        var cliente = await Db.Clientes.FindAsync(ClienteId);
        if (cliente is null) return NotFound();

        cliente.NomeCliente = Input.NomeCliente;
        cliente.Telefone    = Input.Telefone;
        cliente.Celular     = Input.Celular;
        cliente.Rua         = Input.Rua;
        cliente.Numero      = Input.Numero;
        cliente.Complemento = Input.Complemento;
        cliente.Bairro      = Input.Bairro;
        cliente.Cidade      = Input.Cidade;
        cliente.Estado      = Input.Estado;
        cliente.Cep         = Input.Cep;

        if (!string.IsNullOrWhiteSpace(Input.NovaSenha))
            cliente.Senha = BCrypt.Net.BCrypt.HashPassword(Input.NovaSenha);

        await Db.SaveChangesAsync();
        TempData["Success"] = "Dados atualizados com sucesso.";
        return RedirectToPage("Conta");
    }
}
