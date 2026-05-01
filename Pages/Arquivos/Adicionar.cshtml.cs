using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Arquivos;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required] public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? Categoria { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aArquivo")) return SemPermissao();
        await SetLayoutDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        if (!await TemPermissaoAsync("aArquivo")) return SemPermissao();
        if (!ModelState.IsValid) { await SetLayoutDataAsync(); return Page(); }

        string? url = null, path = null, tipo = null, tamanho = null;
        if (file is { Length: > 0 })
        {
            var dir = Path.Combine("wwwroot", "uploads", "docs");
            Directory.CreateDirectory(dir);
            var fileName = Path.GetFileName(file.FileName);
            path = Path.Combine(dir, fileName);
            using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream);
            url     = $"/uploads/docs/{fileName}";
            tipo    = Path.GetExtension(fileName).TrimStart('.').ToUpper();
            tamanho = $"{file.Length / 1024.0:N1} KB";
        }

        Db.Documentos.Add(new Documento
        {
            Nome      = Input.Nome,
            Descricao = Input.Descricao,
            Categoria = Input.Categoria,
            File      = file?.FileName,
            Path      = path,
            Url       = url,
            Tipo      = tipo,
            Tamanho   = tamanho,
            Cadastro  = DateOnly.FromDateTime(DateTime.Today)
        });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Arquivo enviado.";
        return RedirectToPage("/Arquivos/Index");
    }
}
