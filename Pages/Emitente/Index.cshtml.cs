using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace mapos_dotnet.Pages.Emitente;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }
        [Required] public string Nome { get; set; } = string.Empty;
        public string? Cnpj { get; set; }
        public string? Ie { get; set; }
        public string? Rua { get; set; }
        public string? Numero { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? Cep { get; set; }
    }

    public string? LogoUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cEmitente")) return SemPermissao();
        await SetLayoutDataAsync();
        var e = await Db.Emitente.FirstOrDefaultAsync() ?? new Models.Emitente();
        Input = new InputModel { Id = e.Id, Nome = e.Nome, Cnpj = e.Cnpj, Ie = e.Ie, Rua = e.Rua, Numero = e.Numero, Bairro = e.Bairro, Cidade = e.Cidade, Uf = e.Uf, Telefone = e.Telefone, Email = e.Email, Cep = e.Cep };
        LogoUrl = e.UrlLogo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? logo)
    {
        if (!await TemPermissaoAsync("cEmitente")) return SemPermissao();
        if (!ModelState.IsValid) { await SetLayoutDataAsync(); return Page(); }

        var e = await Db.Emitente.FirstOrDefaultAsync();
        if (e is null) { e = new Models.Emitente(); Db.Emitente.Add(e); }

        e.Nome = Input.Nome; e.Cnpj = Input.Cnpj; e.Ie = Input.Ie;
        e.Rua = Input.Rua; e.Numero = Input.Numero; e.Bairro = Input.Bairro;
        e.Cidade = Input.Cidade; e.Uf = Input.Uf; e.Telefone = Input.Telefone;
        e.Email = Input.Email; e.Cep = Input.Cep;

        if (logo is { Length: > 0 })
        {
            var dir = Path.Combine("wwwroot", "uploads", "logo");
            Directory.CreateDirectory(dir);
            var ext  = Path.GetExtension(logo.FileName);
            var path = Path.Combine(dir, $"logo{ext}");
            using var stream = System.IO.File.Create(path);
            await logo.CopyToAsync(stream);
            e.UrlLogo = $"/uploads/logo/logo{ext}";
        }

        await Db.SaveChangesAsync();
        await AuditarAsync("Atualizou dados do emitente");
        TempData["Success"] = "Emitente atualizado.";
        return RedirectToPage();
    }
}
